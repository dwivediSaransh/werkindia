using System.Security.Cryptography;
using IndiaWorks.Application.Features.E01.Create;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Domain.E01;
using Microsoft.Extensions.Logging;
using Xunit;
using CancellationToken = System.Threading.CancellationToken;

namespace IndiaWorks.Application.Tests.Features.E01;

/// <summary>
/// Tests for RefreshTokenRotationService.
/// Uses in-memory fakes for repository dependencies.
/// </summary>
public sealed class RefreshTokenRotationTests
{
    private sealed class FakeUserDeviceRepository : IUserDeviceRepository
    {
        private readonly Dictionary<Guid, UserDevice> _devices = new();
        private readonly Dictionary<string, UserDevice> _byHash = new();

        public void Add(UserDevice device)
        {
            _devices[device.Id] = device;
            _byHash[device.RefreshTokenHash] = device;
        }

        public Task<UserDevice?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
        {
            _byHash.TryGetValue(refreshTokenHash, out var device);
            return Task.FromResult(device);
        }

        public Task<UserDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _devices.TryGetValue(id, out var device);
            return Task.FromResult(device);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddAsync(UserDevice device, CancellationToken cancellationToken = default)
        {
            _devices[device.Id] = device;
            _byHash[device.RefreshTokenHash] = device;
            return Task.CompletedTask;
        }

        public void Clear() => _devices.Clear();
    }

    private sealed class FakeUserSessionRepository : IUserSessionRepository
    {
        private readonly Dictionary<Guid, UserSession> _sessions = new();

        public void Add(UserSession session)
        {
            _sessions[session.Id] = session;
        }

        public Task<UserSession?> GetActiveByUserAndDeviceAsync(
            Guid userId,
            Guid deviceId,
            CancellationToken cancellationToken = default)
        {
            foreach (var session in _sessions.Values)
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
            var result = _sessions.Values
                .Where(s => s.UserId == userId)
                .ToList()
                .AsReadOnly();
            return Task.FromResult<IReadOnlyList<UserSession>>(result);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }

        public void Clear() => _sessions.Clear();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        public void Clear() => Entries.Clear();
    }

    private sealed class MockJwtTokenService : IJwtTokenService
    {
        public List<CreateAccessTokenRequest> Requests { get; } = new();
        public AccessTokenResult LastResult { get; set; } = AccessTokenResult.Ok("test-access-token");

        public AccessTokenResult CreateAccessToken(CreateAccessTokenRequest request)
        {
            Requests.Add(request);
            return LastResult;
        }

        public void Clear() => Requests.Clear();
    }

    private static (RefreshTokenRotationService Service, FakeUserDeviceRepository DeviceRepo, FakeUserSessionRepository SessionRepo, MockJwtTokenService JwtService, TestLogger<RefreshTokenRotationService> Logger)
        CreateSut()
    {
        var deviceRepo = new FakeUserDeviceRepository();
        var sessionRepo = new FakeUserSessionRepository();
        var jwtService = new MockJwtTokenService();
        var logger = new TestLogger<RefreshTokenRotationService>();
        var sut = new RefreshTokenRotationService(deviceRepo, sessionRepo, jwtService, logger);
        return (sut, deviceRepo, sessionRepo, jwtService, logger);
    }

    private static UserDevice CreateDevice(
        Guid userId,
        string? refreshTokenHash = null,
        bool isRevoked = false)
    {
        var hash = refreshTokenHash ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        return UserDevice.Create(
            userId: userId,
            deviceId: Guid.NewGuid().ToString("N"),
            refreshTokenHash: hash,
            deviceName: "Test Device");
    }

    private static UserSession CreateSession(
        Guid userId,
        Guid deviceId,
        bool isRevoked = false,
        bool isExpired = false)
    {
        // Create with a future expiry first (domain validation)
        var session = UserSession.Create(
            userId: userId,
            deviceId: deviceId,
            activePersona: UserSession.Persona.Employer,
            expiresOn: DateTime.UtcNow.AddHours(1));

        // Then manipulate for test scenarios
        if (isRevoked)
        {
            session.Revoke();
        }

        if (isExpired)
        {
            // Directly set the backing field via reflection for testing
            var prop = typeof(UserSession).GetProperty(nameof(UserSession.ExpiresOn));
            if (prop != null)
            {
                prop.SetValue(session, DateTime.UtcNow.AddSeconds(-10));
            }
        }

        return session;
    }

    [Fact]
    public async Task RotateAsync_happy_path_rotates_token_and_returns_new_access_token()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var oldHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var (sut, deviceRepo, sessionRepo, jwtService, logger) = CreateSut();

        var device = CreateDevice(userId, oldHash);
        deviceRepo.Add(device);

        var session = CreateSession(userId, deviceId);
        sessionRepo.Add(session);

        var request = new RefreshTokenRotationRequest(
            UserId: userId,
            DeviceId: deviceId,
            RefreshTokenHash: oldHash);

        // Act
        var result = await sut.RotateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.NewRefreshTokenHash);
        Assert.NotEqual(oldHash, result.NewRefreshTokenHash);

        // Verify access token was requested with correct claims
        Assert.Single(jwtService.Requests);
        var jwtRequest = jwtService.Requests[0];
        Assert.Equal(userId, jwtRequest.Subject);
        Assert.Equal(session.Id, jwtRequest.SessionId);
        Assert.Equal("Employer", jwtRequest.Persona);

        // Verify device was updated with new hash
        var updatedDevice = await deviceRepo.GetByIdAsync(device.Id);
        Assert.NotNull(updatedDevice);
        Assert.Equal(result.NewRefreshTokenHash, updatedDevice.RefreshTokenHash);
        Assert.NotEqual(oldHash, updatedDevice.RefreshTokenHash);

        // Verify session was touched
        var sessions = await sessionRepo.GetAllByUserIdAsync(userId);
        var updatedSession = sessions.First(s => s.Id == session.Id);
        Assert.False(updatedSession.IsRevoked);
    }

    [Fact]
    public async Task RotateAsync_stale_token_returns_token_reuse_detected()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fakeHash = "stale-token-hash-that-does-not-exist";

        var (sut, deviceRepo, sessionRepo, jwtService, logger) = CreateSut();

        var request = new RefreshTokenRotationRequest(
            UserId: userId,
            DeviceId: Guid.NewGuid(),
            RefreshTokenHash: fakeHash);

        // Act
        var result = await sut.RotateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("TOKEN_REUSE_DETECTED", result.ErrorCode);
        Assert.Contains("invalidated", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RotateAsync_device_belongs_to_other_user_returns_forbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var tokenHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var (sut, deviceRepo, sessionRepo, jwtService, logger) = CreateSut();

        // Create device for other user with this token
        var device = CreateDevice(otherUserId, tokenHash);
        deviceRepo.Add(device);

        var request = new RefreshTokenRotationRequest(
            UserId: userId,
            DeviceId: device.Id,
            RefreshTokenHash: tokenHash);

        // Act
        var result = await sut.RotateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    [Fact]
    public async Task RotateAsync_revoked_device_returns_session_revoked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var (sut, deviceRepo, sessionRepo, jwtService, logger) = CreateSut();

        var device = CreateDevice(userId, tokenHash, isRevoked: true);
        deviceRepo.Add(device);

        var request = new RefreshTokenRotationRequest(
            UserId: userId,
            DeviceId: device.Id,
            RefreshTokenHash: tokenHash);

        // Act
        var result = await sut.RotateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_REVOKED", result.ErrorCode);
    }

    [Fact]
    public async Task RotateAsync_revoked_session_returns_session_revoked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var (sut, deviceRepo, sessionRepo, jwtService, logger) = CreateSut();

        var device = CreateDevice(userId, tokenHash);
        deviceRepo.Add(device);

        var session = CreateSession(userId, device.Id, isRevoked: true);
        sessionRepo.Add(session);

        var request = new RefreshTokenRotationRequest(
            UserId: userId,
            DeviceId: device.Id,
            RefreshTokenHash: tokenHash);

        // Act
        var result = await sut.RotateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_REVOKED", result.ErrorCode);
    }

    [Fact]
    public async Task RotateAsync_expired_session_returns_session_revoked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var (sut, deviceRepo, sessionRepo, jwtService, logger) = CreateSut();

        var device = CreateDevice(userId, tokenHash);
        deviceRepo.Add(device);

        var session = CreateSession(userId, device.Id, isExpired: true);
        sessionRepo.Add(session);

        var request = new RefreshTokenRotationRequest(
            UserId: userId,
            DeviceId: device.Id,
            RefreshTokenHash: tokenHash);

        // Act
        var result = await sut.RotateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_REVOKED", result.ErrorCode);
    }

    [Fact]
    public async Task RotateAsync_no_session_found_returns_session_revoked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var (sut, deviceRepo, sessionRepo, jwtService, logger) = CreateSut();

        var device = CreateDevice(userId, tokenHash);
        deviceRepo.Add(device);

        // No session added

        var request = new RefreshTokenRotationRequest(
            UserId: userId,
            DeviceId: device.Id,
            RefreshTokenHash: tokenHash);

        // Act
        var result = await sut.RotateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_REVOKED", result.ErrorCode);
    }

    [Fact]
    public async Task RotateAsync_missing_token_hash_throws_argument_exception()
    {
        // Arrange
        var (sut, deviceRepo, sessionRepo, jwtService, logger) = CreateSut();

        var request = new RefreshTokenRotationRequest(
            UserId: Guid.NewGuid(),
            DeviceId: Guid.NewGuid(),
            RefreshTokenHash: "");

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.RotateAsync(request));
    }

    [Fact]
    public async Task RotateAsync_jwt_fails_returns_unauthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var (sut, deviceRepo, sessionRepo, jwtService, logger) = CreateSut();
        jwtService.LastResult = AccessTokenResult.Fail("INTERNAL_ERROR", "JWT signing failed");

        var device = CreateDevice(userId, tokenHash);
        deviceRepo.Add(device);

        var session = CreateSession(userId, device.Id);
        sessionRepo.Add(session);

        var request = new RefreshTokenRotationRequest(
            UserId: userId,
            DeviceId: device.Id,
            RefreshTokenHash: tokenHash);

        // Act
        var result = await sut.RotateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHENTICATED", result.ErrorCode);
    }

    [Fact]
    public async Task RotateAsync_no_plaintext_tokens_in_logs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var (sut, deviceRepo, sessionRepo, jwtService, logger) = CreateSut();

        var device = CreateDevice(userId, tokenHash);
        deviceRepo.Add(device);

        var session = CreateSession(userId, device.Id);
        sessionRepo.Add(session);

        var request = new RefreshTokenRotationRequest(
            UserId: userId,
            DeviceId: device.Id,
            RefreshTokenHash: tokenHash);

        // Act
        await sut.RotateAsync(request);

        // Assert
        var allLogText = string.Join(" ", logger.Entries.Select(e => e.Message));

        // Full token hash should NOT appear in logs (masked)
        Assert.DoesNotContain(tokenHash, allLogText);

        // No actual JWT tokens in logs
        Assert.DoesNotContain("eyJ", allLogText);
    }
}
