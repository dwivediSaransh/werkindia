namespace IndiaWorks.Contracts.E01;

public sealed record RequestOtpRequest(
    Guid AccountId,
    string Purpose,
    string NormalizedIdentifier,
    string Channel);

public sealed record RequestOtpResponse(
    Guid ChallengeId,
    string Purpose,
    DateTime ExpiresOn,
    int MaxAttempts,
    int ResendCooldownSeconds);

public sealed record RequestOtpErrorResponse(
    string Code,
    string Message);

// Verify endpoint contracts
public sealed record VerifyRequest(
    Guid ChallengeId,
    string Code,
    string NormalizedIdentifier);

public sealed record VerifyResponse(
    Guid AccountId,
    string DisplayName,
    string Provider,
    bool IsNewAccount,
    Guid DeviceId,
    string AccessToken,
    DateTime ExpiresOn);

public sealed record VerifyErrorResponse(
    string Code,
    string Message);

// Refresh endpoint contracts
public sealed record RefreshRequest(
    Guid UserId,
    Guid DeviceId,
    string RefreshTokenHash);

public sealed record RefreshResponse(
    string AccessToken,
    string NewRefreshTokenHash,
    DateTime ExpiresOn);

public sealed record RefreshErrorResponse(
    string Code,
    string Message);

// Logout endpoint contracts
public sealed record LogoutRequest(
    Guid UserId,
    Guid DeviceId,
    string RefreshTokenHash);

public sealed record LogoutResponse(
    bool Success);

public sealed record LogoutErrorResponse(
    string Code,
    string Message);
