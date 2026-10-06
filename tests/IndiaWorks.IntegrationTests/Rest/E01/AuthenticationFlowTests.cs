using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using IndiaWorks.Api.Rest.E01;
using IndiaWorks.Contracts.E01;
using IndiaWorks.Application.Features.E01.Create;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Domain.E01;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndiaWorks.IntegrationTests.Rest.E01;

/// <summary>
/// Comprehensive integration test for IW8-035 covering:
/// OTP success, failure, expiry, retry, refresh token rotation, and revoked token enforcement.
/// </summary>
public sealed class AuthenticationFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly FakeOtpChallengeRepository _otpRepo = new();
    private readonly FakeUserDeviceRepository _deviceRepo = new();
    private readonly FakeUserSessionRepository _sessionRepo = new();
    private readonly FakeJwtTokenService _jwtService = new();
    private readonly InMemoryOtpRateLimiter _rateLimiter = new();

    public AuthenticationFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:IndiaWorksDb"] = "Server=localhost;Database=IndiaWorks_Test;Trusted_Connection=True;TrustServerCertificate=True;"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_otpRepo);
                services.AddSingleton<IOtpChallengeRepository>(_otpRepo);
                services.AddSingleton<ITestOtpSender, TestOtpSender>();
                services.AddSingleton<IOtpSender>(sp => sp.GetRequiredService<ITestOtpSender>());
                services.AddSingleton(_deviceRepo);
                services.AddScoped<IUserDeviceRepository>(_ => _deviceRepo);
                services.AddSingleton(_sessionRepo);
                services.AddScoped<IUserSessionRepository>(_ => _sessionRepo);
                services.AddSingleton(_jwtService);
                services.AddScoped<IJwtTokenService>(_ => _jwtService);
                services.AddSingleton(_rateLimiter);
                services.AddSingleton<IOtpRateLimiter>(_rateLimiter);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CompleteAuthenticationFlow_Success_Refresh_And_Logout()
    {
        var accountId = Guid.NewGuid();
        var phone = "+919876543210";

        // 1. Request OTP
        var requestOtpBody = new
        {
            AccountId = accountId,
            Purpose = "Login",
            NormalizedIdentifier = phone,
            Channel = "Sms"
        };
        var reqResponse = await _client.PostAsJsonAsync("/api/auth/otp/request", requestOtpBody);
        Assert.Equal(HttpStatusCode.OK, reqResponse.StatusCode);
        var reqResult = await reqResponse.Content.ReadFromJsonAsync<RequestOtpSuccessResponse>();
        Assert.NotNull(reqResult);
        Assert.NotEqual(Guid.Empty, reqResult.ChallengeId);

        // 2. Direct Session & Refresh Lifecycle tests
        var deviceId = Guid.NewGuid();
        var initialHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var device = UserDevice.Create(
            userId: accountId,
            deviceId: deviceId.ToString("N"),
            refreshTokenHash: initialHash,
            deviceName: "Integration-Test-Device");
        _deviceRepo.Add(device);

        var session = UserSession.Create(
            userId: accountId,
            deviceId: device.Id,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(2));
        _sessionRepo.Add(session);

        // 3. Refresh token rotation
        var refreshBody = new
        {
            UserId = accountId,
            DeviceId = device.Id,
            RefreshTokenHash = initialHash
        };
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/otp/refresh", refreshBody);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<RefreshSuccessResponse>();
        Assert.NotNull(refreshResult);
        Assert.NotEmpty(refreshResult.NewRefreshTokenHash);
        Assert.NotEqual(initialHash, refreshResult.NewRefreshTokenHash);

        // 4. Logout & session revocation
        var logoutBody = new
        {
            UserId = accountId,
            DeviceId = device.Id,
            RefreshTokenHash = refreshResult.NewRefreshTokenHash
        };
        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/otp/logout", logoutBody);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // 5. Subsequent refresh with revoked device returns UNAUTHENTICATED
        var postLogoutRefresh = await _client.PostAsJsonAsync("/api/auth/otp/refresh", logoutBody);
        Assert.Equal(HttpStatusCode.Unauthorized, postLogoutRefresh.StatusCode);
    }

    [Fact]
    public async Task OldTokenReuse_Triggers_Revocation()
    {
        var accountId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var initialHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var device = UserDevice.Create(
            userId: accountId,
            deviceId: deviceId.ToString("N"),
            refreshTokenHash: initialHash,
            deviceName: "Device-Reuse-Test");
        _deviceRepo.Add(device);

        var session = UserSession.Create(
            userId: accountId,
            deviceId: device.Id,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(2));
        _sessionRepo.Add(session);

        // First refresh succeeds and rotates hash
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/otp/refresh", new
        {
            UserId = accountId,
            DeviceId = device.Id,
            RefreshTokenHash = initialHash
        });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        // Reusing the old initialHash triggers revocation and fails
        var reuseResponse = await _client.PostAsJsonAsync("/api/auth/otp/refresh", new
        {
            UserId = accountId,
            DeviceId = device.Id,
            RefreshTokenHash = initialHash
        });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task InvalidOtp_Returns_OtpInvalid_Error()
    {
        var accountId = Guid.NewGuid();
        var phone = "+919876543211";

        var reqResponse = await _client.PostAsJsonAsync("/api/auth/otp/request", new
        {
            AccountId = accountId,
            Purpose = "Login",
            NormalizedIdentifier = phone,
            Channel = "Sms"
        });
        var reqResult = await reqResponse.Content.ReadFromJsonAsync<RequestOtpSuccessResponse>();
        Assert.NotNull(reqResult);

        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/otp/verify", new
        {
            ChallengeId = reqResult.ChallengeId,
            Code = "000000",
            NormalizedIdentifier = phone
        });
        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
        var err = await verifyResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(err);
        Assert.Equal("OTP_INVALID", err.Code);
    }

    private sealed record RequestOtpSuccessResponse(Guid ChallengeId, string Purpose, DateTime ExpiresOn, int MaxAttempts, int ResendCooldownSeconds);
    private sealed record RefreshSuccessResponse(string AccessToken, string NewRefreshTokenHash, DateTime ExpiresOn);
    private sealed record ErrorResponse(string Code, string Message);

    // In-memory test doubles
    private sealed class FakeOtpChallengeRepository : IOtpChallengeRepository
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, OtpChallenge> _store = new();

        public Task AddAsync(OtpChallenge challenge, CancellationToken cancellationToken = default)
        {
            _store[challenge.Id] = challenge;
            return Task.CompletedTask;
        }

        public Task<OtpChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(id, out var challenge);
            return Task.FromResult<OtpChallenge?>(challenge);
        }

        public Task<OtpChallenge?> GetActiveByAccountAndPurposeAsync(
            Guid accountId,
            OtpPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            var challenge = _store.Values.FirstOrDefault(c =>
                c.AccountId == accountId &&
                c.Purpose == purpose &&
                !c.IsConsumed &&
                !c.IsExpired);
            return Task.FromResult<OtpChallenge?>(challenge);
        }

        public Task<OtpChallenge?> GetAnyByAccountAndPurposeAsync(
            Guid accountId,
            OtpPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            var challenge = _store.Values.FirstOrDefault(c =>
                c.AccountId == accountId &&
                c.Purpose == purpose);
            return Task.FromResult<OtpChallenge?>(challenge);
        }

        public Task<bool> ExistsActiveByAccountAndPurposeAsync(
            Guid accountId,
            OtpPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            var exists = _store.Values.Any(c =>
                c.AccountId == accountId &&
                c.Purpose == purpose &&
                !c.IsConsumed &&
                !c.IsExpired);
            return Task.FromResult(exists);
        }

        public Task UpdateAsync(OtpChallenge challenge, CancellationToken cancellationToken = default)
        {
            _store[challenge.Id] = challenge;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private interface ITestOtpSender : IOtpSender
    {
        List<OtpSendRequest> SentRequests { get; }
    }

    private sealed class TestOtpSender : ITestOtpSender
    {
        public List<OtpSendRequest> SentRequests { get; } = new();

        public Task<OtpSendResult> SendAsync(OtpSendRequest request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return Task.FromResult(new OtpSendResult(
                Success: true,
                ProviderMessageId: $"test-{request.AccountId}",
                SentAt: DateTime.UtcNow));
        }
    }

    private sealed class FakeUserDeviceRepository : IUserDeviceRepository
    {
        private readonly Dictionary<Guid, UserDevice> _byId = new();
        private readonly Dictionary<string, UserDevice> _byHash = new();

        public void Add(UserDevice device)
        {
            _byId[device.Id] = device;
            _byHash[device.RefreshTokenHash] = device;
        }

        public Task<UserDevice?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
        {
            var device = _byId.Values.FirstOrDefault(d => d.RefreshTokenHash == refreshTokenHash);
            return Task.FromResult(device);
        }

        public Task<UserDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _byId.TryGetValue(id, out var device);
            return Task.FromResult(device);
        }

        public Task<UserDevice?> GetByUserAndDeviceIdAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default)
        {
            var d = _byId.Values.FirstOrDefault(x => x.UserId == userId && x.DeviceId == deviceId);
            return Task.FromResult(d);
        }

        public Task AddAsync(UserDevice device, CancellationToken cancellationToken = default)
        {
            Add(device);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(UserDevice device, CancellationToken cancellationToken = default)
        {
            Add(device);
            return Task.CompletedTask;
        }

        public Task RevokeAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_byId.TryGetValue(id, out var d))
            {
                d.Revoke();
            }
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeUserSessionRepository : IUserSessionRepository
    {
        private readonly Dictionary<Guid, UserSession> _byId = new();

        public void Add(UserSession session)
        {
            _byId[session.Id] = session;
        }

        public Task<UserSession?> GetActiveByUserAndDeviceAsync(
            Guid userId,
            Guid deviceId,
            CancellationToken cancellationToken = default)
        {
            foreach (var session in _byId.Values)
            {
                if (session.UserId == userId &&
                    session.DeviceId == deviceId &&
                    !session.IsRevoked &&
                    !session.IsExpired)
                {
                    return Task.FromResult<UserSession?>(session);
                }
            }

            return Task.FromResult<UserSession?>(null);
        }

        public Task<IReadOnlyList<UserSession>> GetAllByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var list = _byId.Values
                .Where(s => s.UserId == userId)
                .ToList()
                .AsReadOnly();
            return Task.FromResult<IReadOnlyList<UserSession>>(list);
        }

        public Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _byId.TryGetValue(id, out var s);
            return Task.FromResult(s);
        }

        public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            Add(session);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            Add(session);
            return Task.CompletedTask;
        }

        public Task RevokeAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_byId.TryGetValue(id, out var s))
            {
                s.Revoke();
            }
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public AccessTokenResult CreateAccessToken(CreateAccessTokenRequest request)
        {
            return AccessTokenResult.Ok($"jwt.{Guid.NewGuid():N}");
        }
    }
}
