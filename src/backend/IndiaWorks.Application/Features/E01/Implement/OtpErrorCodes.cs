namespace IndiaWorks.Application.Features.E01.Implement;

public static class OtpErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Forbidden = "FORBIDDEN";
    public const string OtpExpired = "OTP_EXPIRED";
    public const string OtpInvalid = "OTP_INVALID";
    public const string OtpAttemptsExceeded = "OTP_ATTEMPTS_EXCEEDED";
    public const string RateLimited = "RATE_LIMITED";
    public const string SessionRevoked = "SESSION_REVOKED";
    public const string TokenReuseDetected = "TOKEN_REUSE_DETECTED";
}
