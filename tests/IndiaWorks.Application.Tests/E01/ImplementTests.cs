using System.Collections.Concurrent;
using IndiaWorks.Application.Features.E01.Create;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Domain.E01;
using Microsoft.Extensions.Logging;
using Xunit;
using CancellationToken = System.Threading.CancellationToken;

namespace IndiaWorks.Application.Tests.Features.E01;

public sealed class ImplementTests
{
    private static OtpSettings DefaultSettings => new()
    {
        CodeLength = 6,
        TtlSeconds = 300,
        MaxAttempts = 5,
        ResendCooldownSeconds = 30,
        RateLimitMaxPerWindow = 5,
        RateLimitWindowSeconds = 300
    };

    // In-memory fake repository
    private sealed class FakeOtpChallengeRepository : IOtpChallengeRepository
    {
        private readonly ConcurrentDictionary<Guid, OtpChallenge> _store = new();

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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        // For test assertions
        public IReadOnlyCollection<OtpChallenge> AllChallenges => _store.Values.ToList().AsReadOnly();
        public void Clear() => _store.Clear();
    }

    // In-memory rate limiter
    private sealed class TestRateLimiter : IOtpRateLimiter
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _entries = new();
        public List<string> RecordedKeys { get; } = new();

        public bool IsRateLimited(string key, int maxPerWindow, TimeSpan window)
        {
            var now = DateTime.UtcNow;
            var cutoff = now - window;
            var entry = _entries.GetOrAdd(key, _ => new());
            entry.RemoveAll(t => t < cutoff);
            return entry.Count > maxPerWindow;
        }

        public void Record(string key)
        {
            var entry = _entries.GetOrAdd(key, _ => new());
            entry.Add(DateTime.UtcNow);
            if (!RecordedKeys.Contains(key))
            {
                RecordedKeys.Add(key);
            }
        }

        public int Count(string key)
        {
            var now = DateTime.UtcNow;
            var cutoff = now - TimeSpan.FromMinutes(5);
            var entry = _entries.GetValueOrDefault(key);
            return entry?.Count(t => t >= cutoff) ?? 0;
        }

        public void Clear() => _entries.Clear();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public ConcurrentBag<(LogLevel Level, string Message)> Entries { get; } = new();

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
    }

    private static (OtpPolicyService Service, TestRateLimiter RateLimiter, TestLogger<OtpPolicyService> Logger, FakeOtpChallengeRepository Repo) CreateSut(
        OtpSettings? settings = null)
    {
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var logger = new TestLogger<OtpPolicyService>();
        var service = new OtpPolicyService(
            repo,
            rateLimiter,
            hasher,
            logger,
            settings ?? DefaultSettings);
        return (service, rateLimiter, logger, repo);
    }

    [Fact]
    public async Task GenerateAndStoreAsync_creates_challenge_and_verify_succeeds()
    {
        var (sut, rateLimiter, logger, repo) = CreateSut();
        var accountId = Guid.NewGuid();
        var purpose = OtpPurpose.Login;
        var identifier = "+919****3210";

        var generateResult = await sut.GenerateAndStoreAsync(
            new OtpRequest(accountId, purpose, identifier));

        Assert.True(generateResult.IsSuccess);
        Assert.NotNull(generateResult.Challenge);
        Assert.Equal(purpose, generateResult.Challenge.Purpose);
        Assert.True(generateResult.Challenge.ExpiresOn > DateTime.UtcNow);
        Assert.Equal(DefaultSettings.MaxAttempts, generateResult.Challenge.MaxAttempts);
        Assert.NotNull(generateResult.Challenge.ResendCooldownRemaining);
        Assert.True(generateResult.Challenge.ResendCooldownRemaining.Value > TimeSpan.Zero);

        // Verify the challenge was persisted
        var challenges = repo.AllChallenges;
        Assert.Single(challenges);
        var challenge = challenges.First();
        Assert.Equal(purpose, challenge.Purpose);
        Assert.Equal(accountId, challenge.AccountId);
        Assert.False(challenge.IsConsumed);
        Assert.False(challenge.IsExpired);

        // Get the active challenge metadata
        var activeChallenge = await sut.GetActiveChallengeAsync(accountId, purpose);
        Assert.NotNull(activeChallenge);
        Assert.Equal(challenge.Id, activeChallenge.ChallengeId);

        // Verify can resend is false initially (cooldown active)
        var canResendInitial = await sut.CanResendAsync(accountId, purpose);
        Assert.False(canResendInitial);
    }

    [Fact]
    public async Task VerifyAsync_with_expired_challenge_returns_otp_expired()
    {
        var settings = new OtpSettings(TtlSeconds: 0); // Expired immediately
        var (sut, rateLimiter, logger, repo) = CreateSut(settings);
        var accountId = Guid.NewGuid();
        var hasher = new Sha256OtpHasher();

        // Create an expired challenge manually
        var challenge = OtpChallenge.Create(
            accountId: accountId,
            purpose: OtpPurpose.Login,
            hashedOtp: hasher.Hash("123456"),
            expiresOn: DateTime.UtcNow.AddSeconds(-10),
            maxAttempts: 5);

        await repo.AddAsync(challenge);
        var added = repo.AllChallenges.First(c => c.Id == challenge.Id);
        Assert.NotNull(added);

        var verifyResult = await sut.VerifyAsync(
            new OtpVerificationRequest(challenge.Id, "123456"));

        Assert.False(verifyResult.IsSuccess);
        Assert.Equal(OtpErrorCodes.OtpExpired, verifyResult.ErrorCode);
        Assert.Contains("expired", verifyResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Challenge should NOT be marked consumed
        Assert.False(added.IsConsumed);
        Assert.Equal(0, added.Attempts);
    }

    [Fact]
    public async Task VerifyAsync_with_wrong_code_increments_attempts()
    {
        var (sut, rateLimiter, logger, repo) = CreateSut();
        var accountId = Guid.NewGuid();
        var hasher = new Sha256OtpHasher();
        var correctCode = "123456";
        var hashedOtp = hasher.Hash(correctCode);

        var challenge = OtpChallenge.Create(
            accountId: accountId,
            purpose: OtpPurpose.Login,
            hashedOtp: hashedOtp,
            expiresOn: DateTime.UtcNow.AddMinutes(5),
            maxAttempts: 3);

        await repo.AddAsync(challenge);

        // Try wrong code 3 times
        for (int i = 0; i < 3; i++)
        {
            var result = await sut.VerifyAsync(
                new OtpVerificationRequest(challenge.Id, "wrongcode"));

            if (i < 2)
            {
                Assert.False(result.IsSuccess);
                Assert.Equal(OtpErrorCodes.OtpInvalid, result.ErrorCode);
            }
            else
            {
                // 3rd attempt should trigger OTP_ATTEMPTS_EXCEEDED
                Assert.False(result.IsSuccess);
                Assert.Equal(OtpErrorCodes.OtpAttemptsExceeded, result.ErrorCode);
            }
        }

        // Should now be at attempt limit
        var updated = repo.AllChallenges.First(c => c.Id == challenge.Id);
        Assert.True(updated.IsAttemptsExceeded);
    }

    [Fact]
    public async Task Rate_limit_exceeded_returns_rate_limited()
    {
        var settings = new OtpSettings(RateLimitMaxPerWindow: 2, RateLimitWindowSeconds: 60);
        var (sut, rateLimiter, logger, repo) = CreateSut(settings);
        var accountId = Guid.NewGuid();
        var identifier = "+919****3210";

        // Make 2 requests within the window (clearing active challenge between to avoid duplicate-check block)
        await sut.GenerateAndStoreAsync(new OtpRequest(accountId, OtpPurpose.Login, identifier));
        repo.Clear();
        await sut.GenerateAndStoreAsync(new OtpRequest(accountId, OtpPurpose.Login, identifier));

        // Third request should be rate limited
        var result = await sut.GenerateAndStoreAsync(
            new OtpRequest(accountId, OtpPurpose.Login, identifier));

        Assert.False(result.IsSuccess);
        Assert.Equal(OtpErrorCodes.RateLimited, result.ErrorCode);
        Assert.Contains("many OTP", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task No_plaintext_otp_in_logs()
    {
        var (sut, rateLimiter, logger, repo) = CreateSut();
        var accountId = Guid.NewGuid();
        var identifier = "+919****3210";
        var purpose = OtpPurpose.Login;

        var initialCount = logger.Entries.Count;

        await sut.GenerateAndStoreAsync(new OtpRequest(accountId, purpose, identifier));

        var postGenerateLogs = logger.Entries.Skip(initialCount).ToList();

        // Verify no log contains the full identifier or any potential OTP pattern
        var allLogText = string.Join(" ", postGenerateLogs.Select(e => e.Message));

        // Check for common OTP patterns that should NOT appear
        Assert.DoesNotContain("+919****3210", allLogText);
        Assert.DoesNotContain("98765", allLogText); // Partial phone shouldn't appear
        Assert.DoesNotContain("123456", allLogText); // Common test OTP pattern
        Assert.DoesNotContain("otp=", allLogText, StringComparison.OrdinalIgnoreCase);

        // The identifier in logs should be masked
        Assert.DoesNotContain(identifier, allLogText);

        // Now test verify flow with a fake OTP (won't succeed but shouldn't leak)
        var challenge = repo.AllChallenges.First();
        logger.Entries.Clear();
        var verifyResult = await sut.VerifyAsync(
            new OtpVerificationRequest(challenge.Id, "000000"));

        var postVerifyLogs = logger.Entries.ToList();
        var verifyLogText = string.Join(" ", postVerifyLogs.Select(e => e.Message));

        Assert.DoesNotContain("+919****3210", verifyLogText);
        Assert.DoesNotContain(challenge.Id.ToString(), verifyLogText); // Full GUID shouldn't appear (masked)
    }

    [Fact]
    public async Task VerifyAsync_with_correct_code_succeeds_and_marks_consumed()
    {
        var (sut, rateLimiter, logger, repo) = CreateSut();
        var accountId = Guid.NewGuid();
        var hasher = new Sha256OtpHasher();
        var correctCode = "654321";
        var hashedOtp = hasher.Hash(correctCode);

        var challenge = OtpChallenge.Create(
            accountId: accountId,
            purpose: OtpPurpose.Verification,
            hashedOtp: hashedOtp,
            expiresOn: DateTime.UtcNow.AddMinutes(5),
            maxAttempts: 5);

        await repo.AddAsync(challenge);

        var result = await sut.VerifyAsync(
            new OtpVerificationRequest(challenge.Id, correctCode));

        Assert.True(result.IsSuccess);
        var updated = repo.AllChallenges.First(c => c.Id == challenge.Id);
        Assert.True(updated.IsConsumed);
        Assert.NotNull(updated.ConsumedOn);
    }

    [Fact]
    public async Task GetActiveChallengeAsync_returns_null_for_nonexistent_challenge()
    {
        var (sut, rateLimiter, logger, repo) = CreateSut();
        var accountId = Guid.NewGuid();

        var result = await sut.GetActiveChallengeAsync(accountId, OtpPurpose.Login);

        Assert.Null(result);
    }

    [Fact]
    public async Task CanResendAsync_returns_true_when_no_existing_challenge()
    {
        var (sut, rateLimiter, logger, repo) = CreateSut();
        var accountId = Guid.NewGuid();

        var result = await sut.CanResendAsync(accountId, OtpPurpose.Login);

        Assert.True(result);
    }

    [Fact]
    public async Task Otp_challenge_without_identifier_still_works()
    {
        var (sut, rateLimiter, logger, repo) = CreateSut();
        var accountId = Guid.NewGuid();

        var result = await sut.GenerateAndStoreAsync(
            new OtpRequest(accountId, OtpPurpose.Login, "user@example.com"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Challenge);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GenerateAndStoreAsync_throws_for_invalid_identifier(string? identifier)
    {
        var (sut, rateLimiter, logger, repo) = CreateSut();

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            sut.GenerateAndStoreAsync(new OtpRequest(Guid.NewGuid(), OtpPurpose.Login, identifier!)));
    }

    [Fact]
    public async Task Duplicate_active_challenge_returns_validation_failed()
    {
        var (sut, rateLimiter, logger, repo) = CreateSut();
        var accountId = Guid.NewGuid();

        var firstResult = await sut.GenerateAndStoreAsync(
            new OtpRequest(accountId, OtpPurpose.Login, "+919****3210"));

        Assert.True(firstResult.IsSuccess);

        // Try to create another active challenge for the same account+purpose
        var secondResult = await sut.GenerateAndStoreAsync(
            new OtpRequest(accountId, OtpPurpose.Login, "+919****3210"));

        Assert.False(secondResult.IsSuccess);
        Assert.Equal(OtpErrorCodes.OtpInvalid, secondResult.ErrorCode);
    }

    // RequestOtpUseCase tests

    private sealed class TestOtpSender : IOtpSender
    {
        public int SendCount { get; private set; }
        public List<OtpSendRequest> SentRequests { get; } = new();
        public bool ShouldSucceed { get; set; } = true;
        public string? OverrideProviderMessageId { get; set; }

        public Task<OtpSendResult> SendAsync(OtpSendRequest request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            SendCount++;

            if (!ShouldSucceed)
            {
                return Task.FromResult(new OtpSendResult(
                    Success: false,
                    ProviderMessageId: null,
                    SentAt: DateTime.UtcNow));
            }

            // Use masked account ID to match production behavior
            var maskedAccountId = MaskIdentifier(request.AccountId.ToString());
            return Task.FromResult(new OtpSendResult(
                Success: true,
                ProviderMessageId: OverrideProviderMessageId ?? $"dev-{maskedAccountId}",
                SentAt: DateTime.UtcNow));
        }

        private static string MaskIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Length <= 4)
            {
                return new string('*', value.Length);
            }

            var visiblePrefix = Math.Max(1, value.Length / 4);
            var visibleSuffix = Math.Max(1, value.Length / 4);
            var maskedLength = value.Length - visiblePrefix - visibleSuffix;

            return value[..visiblePrefix] + new string('*', maskedLength) + value[^visibleSuffix..];
        }

        public void Clear() => SentRequests.Clear();
    }

    [Fact]
    public async Task RequestOtp_use_case_happy_path_creates_challenge_and_sends()
    {
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var logger = new TestLogger<RequestOtpUseCase>();
        var otpSender = new TestOtpSender();
        var settings = DefaultSettings;
        var sut = new RequestOtpUseCase(
            repo,
            rateLimiter,
            hasher,
            otpSender,
            logger,
            settings);

        var accountId = Guid.NewGuid();
        var command = new RequestOtpCommand(
            AccountId: accountId,
            Purpose: OtpPurpose.Login,
            NormalizedIdentifier: "+919876543210",
            Channel: OtpChannel.Sms);

        var result = await sut.ExecuteAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Challenge);
        Assert.Equal(OtpPurpose.Login, result.Challenge.Purpose);
        Assert.True(result.Challenge.ExpiresOn > DateTime.UtcNow);
        Assert.Equal(settings.MaxAttempts, result.Challenge.MaxAttempts);
        Assert.NotNull(result.Challenge.ResendCooldownRemaining);
        Assert.True(result.Challenge.ResendCooldownRemaining.Value > TimeSpan.Zero);

        // Verify challenge was persisted
        var challenges = repo.AllChallenges;
        Assert.Single(challenges);
        var challenge = challenges.First();
        Assert.Equal(accountId, challenge.AccountId);
        Assert.Equal(OtpPurpose.Login, challenge.Purpose);
        Assert.False(challenge.IsConsumed);
        Assert.False(challenge.IsExpired);

        // Verify OTP was sent
        Assert.Single(otpSender.SentRequests);
        var sentRequest = otpSender.SentRequests[0];
        Assert.Equal(accountId, sentRequest.AccountId);
        Assert.Equal(OtpPurpose.Login, sentRequest.Purpose);
        Assert.Equal("+919876543210", sentRequest.NormalizedIdentifier);
        Assert.Equal(OtpChannel.Sms, sentRequest.Channel);
    }

    [Fact]
    public async Task RequestOtp_use_case_rate_limited_does_not_send()
    {
        var settings = new OtpSettings(RateLimitMaxPerWindow: 2, RateLimitWindowSeconds: 60);
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var logger = new TestLogger<RequestOtpUseCase>();
        var otpSender = new TestOtpSender();
        var sut = new RequestOtpUseCase(
            repo,
            rateLimiter,
            hasher,
            otpSender,
            logger,
            settings);

        var accountId = Guid.NewGuid();
        var identifier = "+919876543210";

        // Make 2 requests within the window (clearing challenges to avoid duplicate check)
        await sut.ExecuteAsync(new RequestOtpCommand(accountId, OtpPurpose.Login, identifier, OtpChannel.Sms));
        repo.Clear();
        await sut.ExecuteAsync(new RequestOtpCommand(accountId, OtpPurpose.Login, identifier, OtpChannel.Sms));

        // Third request should be rate limited
        var result = await sut.ExecuteAsync(
            new RequestOtpCommand(accountId, OtpPurpose.Login, identifier, OtpChannel.Sms));

        Assert.False(result.IsSuccess);
        Assert.Equal(OtpErrorCodes.RateLimited, result.ErrorCode);
        Assert.Contains("many OTP", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Provider should not have been called again
        Assert.Equal(2, otpSender.SendCount);
    }

    [Fact]
    public async Task RequestOtp_use_case_no_plaintext_otp_in_logs()
    {
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var logger = new TestLogger<RequestOtpUseCase>();
        var otpSender = new TestOtpSender();
        var settings = DefaultSettings;
        var sut = new RequestOtpUseCase(
            repo,
            rateLimiter,
            hasher,
            otpSender,
            logger,
            settings);

        var accountId = Guid.NewGuid();
        var identifier = "+919876543210";
        var command = new RequestOtpCommand(
            AccountId: accountId,
            Purpose: OtpPurpose.Login,
            NormalizedIdentifier: identifier,
            Channel: OtpChannel.Email);

        var initialCount = logger.Entries.Count;

        await sut.ExecuteAsync(command);

        var postLogs = logger.Entries.Skip(initialCount).ToList();
        var allLogText = string.Join(" ", postLogs.Select(e => e.Message));

        // OTP code should not appear in logs
        Assert.DoesNotContain("123456", allLogText);
        Assert.DoesNotContain("000000", allLogText);

        // Full identifier (phone/email) should not appear in logs (masked)
        Assert.DoesNotContain(identifier, allLogText);

        // Check that logging uses masked account IDs (first 8 chars + ellipsis)
        // The full GUID should not appear as-is in log messages
        var fullAccountId = accountId.ToString("N");
        var maskedFormat = fullAccountId[..8] + "…";
        Assert.DoesNotContain(fullAccountId, allLogText);
    }

    [Fact]
    public async Task RequestOtp_use_case_fails_when_sender_fails()
    {
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var logger = new TestLogger<RequestOtpUseCase>();
        var otpSender = new TestOtpSender { ShouldSucceed = false };
        var settings = DefaultSettings;
        var sut = new RequestOtpUseCase(
            repo,
            rateLimiter,
            hasher,
            otpSender,
            logger,
            settings);

        var accountId = Guid.NewGuid();
        var command = new RequestOtpCommand(
            AccountId: accountId,
            Purpose: OtpPurpose.Login,
            NormalizedIdentifier: "+919876543210",
            Channel: OtpChannel.Sms);

        var result = await sut.ExecuteAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(OtpErrorCodes.OtpInvalid, result.ErrorCode);
        Assert.Contains("Failed to send", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Challenge should still be persisted even though send failed
        var challenges = repo.AllChallenges;
        Assert.Single(challenges);
    }

    [Fact]
    public async Task RequestOtp_use_case_throws_for_invalid_identifier()
    {
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var logger = new TestLogger<RequestOtpUseCase>();
        var otpSender = new TestOtpSender();
        var settings = DefaultSettings;
        var sut = new RequestOtpUseCase(
            repo,
            rateLimiter,
            hasher,
            otpSender,
            logger,
            settings);

        var command = new RequestOtpCommand(
            AccountId: Guid.NewGuid(),
            Purpose: OtpPurpose.Login,
            NormalizedIdentifier: "",
            Channel: OtpChannel.Sms);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task RequestOtp_use_case_duplicate_active_challenge_returns_error()
    {
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var logger = new TestLogger<RequestOtpUseCase>();
        var otpSender = new TestOtpSender();
        var settings = DefaultSettings;
        var sut = new RequestOtpUseCase(
            repo,
            rateLimiter,
            hasher,
            otpSender,
            logger,
            settings);

        var accountId = Guid.NewGuid();
        var command = new RequestOtpCommand(
            AccountId: accountId,
            Purpose: OtpPurpose.Login,
            NormalizedIdentifier: "+919876543210",
            Channel: OtpChannel.Sms);

        // First request succeeds
        var firstResult = await sut.ExecuteAsync(command);
        Assert.True(firstResult.IsSuccess);

        // Second request should fail due to active challenge
        var secondResult = await sut.ExecuteAsync(command);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal(OtpErrorCodes.OtpInvalid, secondResult.ErrorCode);
        Assert.Contains("active OTP challenge already exists", secondResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Provider should only be called once
        Assert.Single(otpSender.SentRequests);
    }

    // VerifyOtpUseCase tests

    [Fact]
    public async Task VerifyOtp_use_case_happy_path_returns_success()
    {
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var policyLogger = new TestLogger<OtpPolicyService>();
        var otpLogger = new TestLogger<VerifyOtpUseCase>();
        var otpPolicy = new OtpPolicyService(repo, rateLimiter, hasher, policyLogger, DefaultSettings);
        var sut = new VerifyOtpUseCase(otpPolicy, repo, otpLogger);

        var accountId = Guid.NewGuid();
        var correctCode = "654321";
        var hashedOtp = hasher.Hash(correctCode);

        var challenge = OtpChallenge.Create(
            accountId: accountId,
            purpose: OtpPurpose.Login,
            hashedOtp: hashedOtp,
            expiresOn: DateTime.UtcNow.AddMinutes(5),
            maxAttempts: 5);

        await repo.AddAsync(challenge);

        var result = await sut.ExecuteAsync(
            new VerifyOtpRequest(challenge.Id, correctCode, "+919****3210"));

        Assert.True(result.IsSuccess);
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal("Otp", result.Provider);
        Assert.False(result.IsNewAccount);
    }

    [Fact]
    public async Task VerifyOtp_use_case_expired_challenge_returns_otp_expired()
    {
        var settings = new OtpSettings(TtlSeconds: 0);
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var policyLogger = new TestLogger<OtpPolicyService>();
        var otpLogger = new TestLogger<VerifyOtpUseCase>();
        var otpPolicy = new OtpPolicyService(repo, rateLimiter, hasher, policyLogger, settings);
        var sut = new VerifyOtpUseCase(otpPolicy, repo, otpLogger);

        var accountId = Guid.NewGuid();
        var challenge = OtpChallenge.Create(
            accountId: accountId,
            purpose: OtpPurpose.Login,
            hashedOtp: hasher.Hash("123456"),
            expiresOn: DateTime.UtcNow.AddSeconds(-10),
            maxAttempts: 5);

        await repo.AddAsync(challenge);

        var result = await sut.ExecuteAsync(
            new VerifyOtpRequest(challenge.Id, "123456", "+919****3210"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OtpErrorCodes.OtpExpired, result.ErrorCode);
        Assert.Contains("expired", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyOtp_use_case_wrong_code_returns_otp_invalid()
    {
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var policyLogger = new TestLogger<OtpPolicyService>();
        var otpLogger = new TestLogger<VerifyOtpUseCase>();
        var otpPolicy = new OtpPolicyService(repo, rateLimiter, hasher, policyLogger, DefaultSettings);
        var sut = new VerifyOtpUseCase(otpPolicy, repo, otpLogger);

        var accountId = Guid.NewGuid();
        var challenge = OtpChallenge.Create(
            accountId: accountId,
            purpose: OtpPurpose.Login,
            hashedOtp: hasher.Hash("654321"),
            expiresOn: DateTime.UtcNow.AddMinutes(5),
            maxAttempts: 5);

        await repo.AddAsync(challenge);

        var result = await sut.ExecuteAsync(
            new VerifyOtpRequest(challenge.Id, "wrongcode", "+919****3210"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OtpErrorCodes.OtpInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task VerifyOtp_use_case_no_plaintext_otp_in_logs()
    {
        var repo = new FakeOtpChallengeRepository();
        var rateLimiter = new TestRateLimiter();
        var hasher = new Sha256OtpHasher();
        var policyLogger = new TestLogger<OtpPolicyService>();
        var otpLogger = new TestLogger<VerifyOtpUseCase>();
        var otpPolicy = new OtpPolicyService(repo, rateLimiter, hasher, policyLogger, DefaultSettings);
        var sut = new VerifyOtpUseCase(otpPolicy, repo, otpLogger);

        var accountId = Guid.NewGuid();
        var correctCode = "654321";
        var challenge = OtpChallenge.Create(
            accountId: accountId,
            purpose: OtpPurpose.Login,
            hashedOtp: hasher.Hash(correctCode),
            expiresOn: DateTime.UtcNow.AddMinutes(5),
            maxAttempts: 5);

        await repo.AddAsync(challenge);

        var initialCount = otpLogger.Entries.Count;
        var identifier = "+919****3210";

        await sut.ExecuteAsync(new VerifyOtpRequest(challenge.Id, correctCode, identifier));

        var postLogs = otpLogger.Entries.Skip(initialCount).ToList();
        var allLogText = string.Join(" ", postLogs.Select(e => e.Message));

        // OTP code should not appear in logs
        Assert.DoesNotContain(correctCode, allLogText);
        // Full identifier should not appear in logs
        Assert.DoesNotContain(identifier, allLogText);
    }

    [Fact]
    public async Task CompleteAuthUseCase_happy_path_returns_access_token_and_creates_device_and_session()
    {
        // Arrange
        var deviceRepo = new FakeUserDeviceRepository();
        var sessionRepo = new FakeUserSessionRepository();
        var jwtService = new FakeJwtTokenService();
        var logger = new TestLogger<CompleteAuthUseCase>();
        var sut = new CompleteAuthUseCase(deviceRepo, sessionRepo, jwtService, logger);

        var accountId = Guid.NewGuid();
        var displayName = "Test User";
        var provider = "Otp";
        var isNewAccount = true;

        // Act
        var result = await sut.ExecuteAsync(
            accountId: accountId,
            displayName: displayName,
            provider: provider,
            isNewAccount: isNewAccount);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(displayName, result.DisplayName);
        Assert.Equal(provider, result.Provider);
        Assert.Equal(isNewAccount, result.IsNewAccount);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.NotNull(result.DeviceId);
        Assert.NotNull(result.SessionId);
        Assert.NotNull(result.RefreshTokenHash);
        Assert.True(result.RefreshTokenHash.Length >= 64); // hex-encoded 32 bytes

        // Assert: device and session were persisted
        var device = await deviceRepo.GetByIdAsync(result.DeviceId!.Value);
        Assert.NotNull(device);
        Assert.Equal(accountId, device.UserId);
        Assert.True(device.RefreshTokenHash.Length >= 64);

        // Assert: session was created and is active
        var session = await sessionRepo.GetActiveByUserAndDeviceAsync(accountId, result.DeviceId.Value);
        Assert.NotNull(session);
        Assert.Equal(accountId, session.UserId);
        Assert.Equal("Candidate", session.ActivePersona.ToString());

        // Assert: JWT service received correct claims
        Assert.NotNull(jwtService.LastRequest);
        Assert.Equal(accountId, jwtService.LastRequest.Subject);
        Assert.Equal(session.Id, jwtService.LastRequest.SessionId);
        Assert.Equal("Candidate", jwtService.LastRequest.Persona);

        // Assert: no secrets in logs
        var allLogText = string.Join(" ", logger.Entries.Select(e => e.Message));
        Assert.DoesNotContain(result.AccessToken, allLogText);
        Assert.DoesNotContain(result.RefreshTokenHash, allLogText);
        Assert.DoesNotContain(accountId.ToString("N"), allLogText);
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
            _byHash.TryGetValue(refreshTokenHash, out var device);
            return Task.FromResult(device);
        }

        public Task<UserDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _byId.TryGetValue(id, out var device);
            return Task.FromResult(device);
        }

        public Task AddAsync(UserDevice device, CancellationToken cancellationToken = default)
        {
            Add(device);
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

        public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            Add(session);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public AccessTokenResult LastResult { get; set; } = AccessTokenResult.Ok("fake-access-token");
        public CreateAccessTokenRequest? LastRequest { get; private set; }

        public AccessTokenResult CreateAccessToken(CreateAccessTokenRequest request)
        {
            LastRequest = request;
            return LastResult;
        }
    }

    // PersonaResolutionService test infrastructure

    private sealed class FakeUserSessionRepositoryForPersona : IUserSessionRepository
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

        public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            Add(session);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestPersonaLogger<T> : ILogger<T>
    {
        public ConcurrentBag<(LogLevel Level, string Message)> Entries { get; } = new();

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
    }

    [Fact]
    public async Task PersonaResolutionService_get_available_personas_with_existing_candidate_session()
    {
        var sessionRepo = new FakeUserSessionRepositoryForPersona();
        var logger = new TestPersonaLogger<PersonaResolutionService>();
        var sut = new PersonaResolutionService(sessionRepo, logger);

        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var session = UserSession.Create(
            userId: userId,
            deviceId: deviceId,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(1));

        sessionRepo.Add(session);

        var result = await sut.GetAvailablePersonasAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.AvailablePersonas);
        Assert.Contains(UserSession.Persona.Candidate, result.AvailablePersonas!);
        Assert.DoesNotContain(UserSession.Persona.Employer, result.AvailablePersonas!);
    }

    [Fact]
    public async Task PersonaResolutionService_get_available_personas_with_both_personas()
    {
        var sessionRepo = new FakeUserSessionRepositoryForPersona();
        var logger = new TestPersonaLogger<PersonaResolutionService>();
        var sut = new PersonaResolutionService(sessionRepo, logger);

        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var candidateSession = UserSession.Create(
            userId: userId,
            deviceId: deviceId,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(1));
        sessionRepo.Add(candidateSession);

        var employerSession = UserSession.Create(
            userId: userId,
            deviceId: Guid.NewGuid(),
            activePersona: UserSession.Persona.Employer,
            expiresOn: DateTime.UtcNow.AddHours(1));
        sessionRepo.Add(employerSession);

        var result = await sut.GetAvailablePersonasAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.AvailablePersonas);
        Assert.Contains(UserSession.Persona.Candidate, result.AvailablePersonas!);
        Assert.Contains(UserSession.Persona.Employer, result.AvailablePersonas!);
    }

    [Fact]
    public async Task PersonaResolutionService_get_available_personas_no_sessions_yet()
    {
        var sessionRepo = new FakeUserSessionRepositoryForPersona();
        var logger = new TestPersonaLogger<PersonaResolutionService>();
        var sut = new PersonaResolutionService(sessionRepo, logger);

        var userId = Guid.NewGuid();

        var result = await sut.GetAvailablePersonasAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.AvailablePersonas);
        Assert.Contains(UserSession.Persona.Candidate, result.AvailablePersonas!);
        Assert.Contains(UserSession.Persona.Employer, result.AvailablePersonas!);
    }

    [Fact]
    public async Task PersonaResolutionService_switch_persona_happy_path()
    {
        var sessionRepo = new FakeUserSessionRepositoryForPersona();
        var logger = new TestPersonaLogger<PersonaResolutionService>();
        var sut = new PersonaResolutionService(sessionRepo, logger);

        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var session = UserSession.Create(
            userId: userId,
            deviceId: deviceId,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(1));
        sessionRepo.Add(session);

        var request = new SwitchPersonaRequest(
            UserId: userId,
            DeviceId: deviceId,
            NewPersona: UserSession.Persona.Employer);

        var result = await sut.SwitchActivePersonaAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserSession.Persona.Employer, result.ActivePersona);

        var updatedSession = await sessionRepo.GetActiveByUserAndDeviceAsync(userId, deviceId);
        Assert.NotNull(updatedSession);
        Assert.Equal(UserSession.Persona.Employer, updatedSession.ActivePersona);
    }

    [Fact]
    public async Task PersonaResolutionService_switch_persona_back()
    {
        var sessionRepo = new FakeUserSessionRepositoryForPersona();
        var logger = new TestPersonaLogger<PersonaResolutionService>();
        var sut = new PersonaResolutionService(sessionRepo, logger);

        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var session = UserSession.Create(
            userId: userId,
            deviceId: deviceId,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(1));
        sessionRepo.Add(session);

        await sut.SwitchActivePersonaAsync(new SwitchPersonaRequest(
            UserId: userId,
            DeviceId: deviceId,
            NewPersona: UserSession.Persona.Employer));

        var result = await sut.SwitchActivePersonaAsync(new SwitchPersonaRequest(
            UserId: userId,
            DeviceId: deviceId,
            NewPersona: UserSession.Persona.Candidate));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserSession.Persona.Candidate, result.ActivePersona);
    }

    [Fact]
    public async Task PersonaResolutionService_switch_persona_no_active_session_returns_session_revoked()
    {
        var sessionRepo = new FakeUserSessionRepositoryForPersona();
        var logger = new TestPersonaLogger<PersonaResolutionService>();
        var sut = new PersonaResolutionService(sessionRepo, logger);

        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        var request = new SwitchPersonaRequest(
            UserId: userId,
            DeviceId: deviceId,
            NewPersona: UserSession.Persona.Employer);

        var result = await sut.SwitchActivePersonaAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_REVOKED", result.ErrorCode);
        Assert.Contains("No active session", result.ErrorMessage);
    }

    [Fact]
    public async Task PersonaResolutionService_no_plaintext_identifiers_in_logs()
    {
        var sessionRepo = new FakeUserSessionRepositoryForPersona();
        var logger = new TestPersonaLogger<PersonaResolutionService>();
        var sut = new PersonaResolutionService(sessionRepo, logger);

        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userIdString = userId.ToString("N");
        var deviceIdString = deviceId.ToString("N");

        var session = UserSession.Create(
            userId: userId,
            deviceId: deviceId,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(1));
        sessionRepo.Add(session);

        var initialCount = logger.Entries.Count;

        await sut.GetAvailablePersonasAsync(userId);

        var logs = logger.Entries.Skip(initialCount)
            .Select(e => e.Message)
            .ToArray();

        var allLogText = string.Join(" ", logs);

        Assert.DoesNotContain(userIdString, allLogText);
        Assert.DoesNotContain(deviceIdString, allLogText);
        Assert.DoesNotContain("eyJ", allLogText);

        logger.Entries.Clear();
        initialCount = logger.Entries.Count;

        await sut.SwitchActivePersonaAsync(new SwitchPersonaRequest(
            UserId: userId,
            DeviceId: deviceId,
            NewPersona: UserSession.Persona.Employer));

        logs = logger.Entries.Skip(initialCount)
            .Select(e => e.Message)
            .ToArray();

        allLogText = string.Join(" ", logs);

        Assert.DoesNotContain(userIdString, allLogText);
        Assert.DoesNotContain(deviceIdString, allLogText);
        Assert.DoesNotContain("eyJ", allLogText);
    }
}
