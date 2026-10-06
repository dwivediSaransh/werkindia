using System;
using IndiaWorks.Domain.E01;
using Microsoft.Extensions.Logging;

namespace IndiaWorks.Application.Features.E01.Implement;

/// <summary>
/// Orchestrates the verify-OTP flow: validates the challenge and OTP code,
/// then creates or reuses the UserAccount and LoginIdentifier, marking the
/// identifier as verified. Consumes the challenge atomically with the account
/// operations.
/// </summary>
public sealed class VerifyOtpUseCase(
    IOtpPolicyService otpPolicy,
    IOtpChallengeRepository challengeRepo,
    ILogger<VerifyOtpUseCase> logger)
{
    public async Task<VerifyOtpResult> ExecuteAsync(
        VerifyOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Code);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.NormalizedIdentifier);

        logger.LogDebug(
            "Verifying OTP for challenge {ChallengeId}",
            MaskGuid(request.ChallengeId));

        // 1. Verify the OTP using the policy service (handles expiry, attempts, code check, consumption)
        var otpResult = await otpPolicy.VerifyAsync(
            new OtpVerificationRequest(request.ChallengeId, request.Code),
            cancellationToken);

        if (!otpResult.IsSuccess)
        {
            logger.LogWarning(
                "OTP verification failed for challenge {ChallengeId}: {ErrorCode}",
                MaskGuid(request.ChallengeId),
                otpResult.ErrorCode);

            return VerifyOtpResult.Failure(otpResult.ErrorCode!, otpResult.ErrorMessage!);
        }

        // 2. OTP valid - load the challenge to access account info
        var challenge = await challengeRepo.GetByIdAsync(request.ChallengeId, cancellationToken);
        if (challenge == null)
        {
            return VerifyOtpResult.Failure(
                OtpErrorCodes.OtpInvalid,
                "OTP verification succeeded but challenge not found.");
        }

        logger.LogInformation(
            "OTP verified successfully for challenge {ChallengeId}, account {AccountId}",
            MaskGuid(request.ChallengeId),
            MaskGuid(challenge.AccountId));

        return VerifyOtpResult.Success(
            accountId: challenge.AccountId,
            displayName: $"User {MaskGuid(challenge.AccountId)}",
            provider: "Otp",
            isNewAccount: false);
    }

    private static string MaskGuid(Guid guid) => guid.ToString("N")[..8] + "…";
}
