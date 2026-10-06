namespace IndiaWorks.Application.Features.E01.Implement;

public sealed record OtpRequest(
    Guid AccountId,
    Domain.E01.OtpPurpose Purpose,
    string NormalizedIdentifier);

public sealed record OtpChallengeMetadata(
    Guid ChallengeId,
    Domain.E01.OtpPurpose Purpose,
    DateTime ExpiresOn,
    int MaxAttempts,
    TimeSpan? ResendCooldownRemaining);

public sealed record OtpVerificationRequest(
    Guid ChallengeId,
    string Code,
    CancellationToken CancellationToken = default);

public sealed record OtpResult(
    bool IsSuccess,
    string? Otp = null,
    OtpChallengeMetadata? Challenge = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static OtpResult Success(
        string? otp = null,
        OtpChallengeMetadata? challenge = null,
        string? errorMessage = null)
    {
        return new OtpResult(
            IsSuccess: true,
            Otp: otp,
            Challenge: challenge,
            ErrorCode: null,
            ErrorMessage: errorMessage);
    }

    public static OtpResult Failure(string errorCode, string errorMessage)
    {
        return new OtpResult(
            IsSuccess: false,
            Otp: null,
            Challenge: null,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
    }
}

public interface IOtpPolicyService
{
    Task<OtpResult> GenerateAndStoreAsync(OtpRequest request, CancellationToken cancellationToken = default);
    Task<OtpResult> VerifyAsync(OtpVerificationRequest request, CancellationToken cancellationToken = default);
    Task<OtpChallengeMetadata?> GetActiveChallengeAsync(Guid accountId, Domain.E01.OtpPurpose purpose, CancellationToken cancellationToken = default);
    Task<bool> CanResendAsync(Guid accountId, Domain.E01.OtpPurpose purpose, CancellationToken cancellationToken = default);
}

public interface IOtpRateLimiter
{
    bool IsRateLimited(string key, int maxPerWindow, TimeSpan window);
    void Record(string key);
}

public interface IOtpHasher
{
    string Hash(string code);
    bool Verify(string code, string hashedCode);
}
