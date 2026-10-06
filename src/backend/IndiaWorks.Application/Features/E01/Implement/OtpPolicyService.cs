using System.Security.Cryptography;
using System.Text;
using IndiaWorks.Domain.E01;
using Microsoft.Extensions.Logging;

namespace IndiaWorks.Application.Features.E01.Implement;

public sealed class OtpPolicyService(
    IOtpChallengeRepository challengeRepo,
    IOtpRateLimiter rateLimiter,
    IOtpHasher hasher,
    ILogger<OtpPolicyService> logger,
    OtpSettings settings)
    : IOtpPolicyService
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    public async Task<OtpResult> GenerateAndStoreAsync(OtpRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.NormalizedIdentifier);

        logger.LogDebug("Generating OTP for account {AccountId} purpose {Purpose}", MaskGuid(request.AccountId), request.Purpose);

        // 1. Record rate limit attempt first (before duplicate check, so all attempts count)
        var rateLimitKey = GetRateLimitKey(request.AccountId, request.Purpose, request.NormalizedIdentifier);
        rateLimiter.Record(rateLimitKey);

        // 2. Rate limiting check
        if (rateLimiter.IsRateLimited(rateLimitKey, settings.RateLimitMaxPerWindow, settings.RateLimitWindow))
        {
            logger.LogWarning("Rate limit exceeded for account {AccountId}", MaskGuid(request.AccountId));
            return OtpResult.Failure(OtpErrorCodes.RateLimited, "Too many OTP requests. Please try again later.");
        }

        // 3. Prevent duplicate active challenge
        if (await challengeRepo.ExistsActiveByAccountAndPurposeAsync(request.AccountId, request.Purpose, cancellationToken))
        {
            logger.LogWarning("Active OTP challenge already exists for account {AccountId} purpose {Purpose}", MaskGuid(request.AccountId), request.Purpose);
            return OtpResult.Failure(OtpErrorCodes.OtpInvalid, "An active OTP challenge already exists for this account and purpose.");
        }

        // 4. Generate OTP code
        var otpCode = GenerateOtpCode(settings.CodeLength);

        // 5. Hash the OTP
        var hashedOtp = hasher.Hash(otpCode);

        // 6. Calculate times
        var now = DateTime.UtcNow;
        var expiresOn = now.Add(settings.Ttl);
        var resendWindowStartsOn = now.Add(settings.ResendCooldown);

        // 7. Create challenge
        var challenge = OtpChallenge.Create(
            accountId: request.AccountId,
            purpose: request.Purpose,
            hashedOtp: hashedOtp,
            expiresOn: expiresOn,
            maxAttempts: settings.MaxAttempts,
            resendWindowStartsOn: resendWindowStartsOn);

        // 8. Persist
        await challengeRepo.AddAsync(challenge, cancellationToken);

        logger.LogInformation(
            "OTP generated for account {AccountId}, purpose {Purpose}, challenge {ChallengeId}, expires {ExpiresOn}",
            MaskGuid(request.AccountId),
            request.Purpose,
            MaskGuid(challenge.Id),
            expiresOn);

        // Return metadata (never the OTP code to callers)
        return OtpResult.Success(
            challenge: new OtpChallengeMetadata(
                ChallengeId: challenge.Id,
                Purpose: challenge.Purpose,
                ExpiresOn: challenge.ExpiresOn,
                MaxAttempts: challenge.MaxAttempts,
                ResendCooldownRemaining: settings.ResendCooldown));
    }

    public async Task<OtpResult> VerifyAsync(OtpVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var challenge = await challengeRepo.GetByIdAsync(request.ChallengeId, cancellationToken);

        if (challenge == null)
        {
            logger.LogWarning("OTP verification failed: challenge {ChallengeId} not found", MaskGuid(request.ChallengeId));
            return OtpResult.Failure(OtpErrorCodes.OtpInvalid, "Invalid OTP challenge.");
        }

        // Check expiration
        if (challenge.IsExpired)
        {
            logger.LogWarning("OTP verification failed: challenge {ChallengeId} expired", MaskGuid(request.ChallengeId));
            return OtpResult.Failure(OtpErrorCodes.OtpExpired, "This OTP has expired. Please request a new one.");
        }

        // Check if already consumed
        if (challenge.IsConsumed)
        {
            logger.LogWarning("OTP verification failed: challenge {ChallengeId} already consumed", MaskGuid(request.ChallengeId));
            return OtpResult.Failure(OtpErrorCodes.OtpInvalid, "This OTP has already been used.");
        }

        // Check attempt limit
        if (challenge.IsAttemptsExceeded)
        {
            logger.LogWarning("OTP verification failed: challenge {ChallengeId} attempts exceeded", MaskGuid(request.ChallengeId));
            return OtpResult.Failure(OtpErrorCodes.OtpAttemptsExceeded, "Maximum OTP attempts exceeded. Please request a new one.");
        }

        // Verify the code
        if (!hasher.Verify(request.Code, challenge.HashedOtp))
        {
            challenge.IncrementAttempts();
            await challengeRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "OTP verification failed: invalid code for challenge {ChallengeId}, attempts now {Attempts}/{MaxAttempts}",
                MaskGuid(request.ChallengeId),
                challenge.Attempts,
                challenge.MaxAttempts);

            if (challenge.IsAttemptsExceeded)
            {
                return OtpResult.Failure(OtpErrorCodes.OtpAttemptsExceeded, "Maximum OTP attempts exceeded. Please request a new one.");
            }

            return OtpResult.Failure(OtpErrorCodes.OtpInvalid, "Invalid OTP code.");
        }

        // Valid OTP - mark consumed
        challenge.MarkConsumed();
        await challengeRepo.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "OTP verification succeeded for challenge {ChallengeId}, account {AccountId}",
            MaskGuid(request.ChallengeId),
            MaskGuid(challenge.AccountId));

        // In production, this would trigger the next step (e.g., create session)
        // For now, return success without exposing OTP code
        return OtpResult.Success(challenge: new OtpChallengeMetadata(
            ChallengeId: challenge.Id,
            Purpose: challenge.Purpose,
            ExpiresOn: challenge.ExpiresOn,
            MaxAttempts: challenge.MaxAttempts,
            ResendCooldownRemaining: null));
    }

    public async Task<OtpChallengeMetadata?> GetActiveChallengeAsync(
        Guid accountId,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var challenge = await challengeRepo.GetActiveByAccountAndPurposeAsync(accountId, purpose, cancellationToken);

        if (challenge == null)
        {
            return null;
        }

        return new OtpChallengeMetadata(
            ChallengeId: challenge.Id,
            Purpose: challenge.Purpose,
            ExpiresOn: challenge.ExpiresOn,
            MaxAttempts: challenge.MaxAttempts,
            ResendCooldownRemaining: CalculateResendCooldownRemaining(challenge));
    }

    public async Task<bool> CanResendAsync(
        Guid accountId,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var challenge = await challengeRepo.GetAnyByAccountAndPurposeAsync(accountId, purpose, cancellationToken);

        if (challenge == null)
        {
            return true; // No existing challenge, can send
        }

        return challenge.CanResend;
    }

    private static string GenerateOtpCode(int length)
    {
        var chars = new char[length];
        var bytes = new byte[length];
        Rng.GetBytes(bytes);

        for (int i = 0; i < length; i++)
        {
            chars[i] = (char)('0' + (bytes[i] % 10));
        }

        return new string(chars);
    }

    private static string GetRateLimitKey(Guid accountId, OtpPurpose purpose, string normalizedIdentifier)
    {
        // Combine account + purpose + identifier for scoped rate limiting
        var key = $"{accountId:N}-{purpose}-{normalizedIdentifier}";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash)[..32]; // First 32 chars of hex hash
    }

    private static TimeSpan? CalculateResendCooldownRemaining(OtpChallenge challenge)
    {
        if (!challenge.ResendWindowStartsOn.HasValue)
        {
            return null;
        }

        var remaining = challenge.ResendWindowStartsOn.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private static string MaskGuid(Guid guid) => guid.ToString("N")[..8] + "…";
}
