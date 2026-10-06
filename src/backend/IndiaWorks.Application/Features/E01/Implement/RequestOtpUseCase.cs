using System.Security.Cryptography;
using IndiaWorks.Application.Features.E01.Create;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Domain.E01;
using Microsoft.Extensions.Logging;

namespace IndiaWorks.Application.Features.E01.Implement;

/// <summary>
/// Orchestrates the request-OTP flow: validates input, applies rate-limit and
/// duplicate-challenge policy, generates a hashed OTP challenge, sends the OTP
/// through IOtpSender, and returns challenge metadata (never the plaintext OTP).
/// </summary>
public sealed class RequestOtpUseCase(
    IOtpChallengeRepository challengeRepo,
    IOtpRateLimiter rateLimiter,
    IOtpHasher hasher,
    IOtpSender otpSender,
    ILogger<RequestOtpUseCase> logger,
    OtpSettings settings)
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    public async Task<OtpResult> ExecuteAsync(RequestOtpCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.NormalizedIdentifier);

        logger.LogDebug(
            "Request OTP for account {AccountId} purpose {Purpose} channel {Channel}",
            MaskGuid(command.AccountId),
            command.Purpose,
            command.Channel);

        // 1. Record rate limit attempt first (before duplicate check, so all attempts count)
        var rateLimitKey = GetRateLimitKey(command.AccountId, command.Purpose, command.NormalizedIdentifier);
        rateLimiter.Record(rateLimitKey);

        // 2. Rate limiting check
        if (rateLimiter.IsRateLimited(rateLimitKey, settings.RateLimitMaxPerWindow, settings.RateLimitWindow))
        {
            logger.LogWarning("Rate limit exceeded for account {AccountId}", MaskGuid(command.AccountId));
            return OtpResult.Failure(OtpErrorCodes.RateLimited, "Too many OTP requests. Please try again later.");
        }

        // 3. Prevent duplicate active challenge
        if (await challengeRepo.ExistsActiveByAccountAndPurposeAsync(command.AccountId, command.Purpose, cancellationToken))
        {
            logger.LogWarning(
                "Active OTP challenge already exists for account {AccountId} purpose {Purpose}",
                MaskGuid(command.AccountId),
                command.Purpose);
            return OtpResult.Failure(OtpErrorCodes.OtpInvalid, "An active OTP challenge already exists for this account and purpose.");
       }

        // 4. Generate OTP code and hash it
        var otpCode = GenerateOtpCode(settings.CodeLength);
        var hashedOtp = hasher.Hash(otpCode);

        // 5. Calculate times
        var now = DateTime.UtcNow;
        var expiresOn = now.Add(settings.Ttl);
        var resendWindowStartsOn = now.Add(settings.ResendCooldown);

        // 6. Create challenge
        var challenge = OtpChallenge.Create(
            accountId: command.AccountId,
            purpose: command.Purpose,
            hashedOtp: hashedOtp,
            expiresOn: expiresOn,
            maxAttempts: settings.MaxAttempts,
            resendWindowStartsOn: resendWindowStartsOn);

        // 7. Persist challenge
        await challengeRepo.AddAsync(challenge, cancellationToken);

        logger.LogInformation(
            "OTP challenge created for account {AccountId}, purpose {Purpose}, challenge {ChallengeId}, expires {ExpiresOn}",
            MaskGuid(command.AccountId),
            command.Purpose,
            MaskGuid(challenge.Id),
            expiresOn);

        // 8. Send OTP via IOtpSender (never log the OTP code)
        var sendRequest = new OtpSendRequest(
            AccountId: command.AccountId,
            Purpose: command.Purpose,
            NormalizedIdentifier: command.NormalizedIdentifier,
            Channel: command.Channel);

        var sendResult = await otpSender.SendAsync(sendRequest, cancellationToken);

        if (!sendResult.Success)
        {
            logger.LogWarning(
                "OTP send failed for challenge {ChallengeId}, provider message: {ProviderMessage}",
                MaskGuid(challenge.Id),
                sendResult.ProviderMessageId);

            return OtpResult.Failure(
                OtpErrorCodes.OtpInvalid,
                "Failed to send OTP. Please try again.");
        }

        logger.LogInformation(
            "OTP sent for challenge {ChallengeId}, provider message {ProviderMessageId}",
            MaskGuid(challenge.Id),
            sendResult.ProviderMessageId);

        // 9. Return challenge metadata (never the OTP code)
        return OtpResult.Success(
            challenge: new OtpChallengeMetadata(
                ChallengeId: challenge.Id,
                Purpose: challenge.Purpose,
                ExpiresOn: challenge.ExpiresOn,
                MaxAttempts: challenge.MaxAttempts,
                ResendCooldownRemaining: settings.ResendCooldown));
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
        var key = $"{accountId:N}-{purpose}-{normalizedIdentifier}";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash)[..32];
    }

    private static string MaskGuid(Guid guid) => guid.ToString("N")[..8] + "…";
}

public sealed record RequestOtpCommand(
    Guid AccountId,
    OtpPurpose Purpose,
    string NormalizedIdentifier,
    OtpChannel Channel);
