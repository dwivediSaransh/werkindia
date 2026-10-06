namespace IndiaWorks.Application.Features.E01.Implement;

public sealed record VerifyOtpResult(
    bool IsSuccess,
    Guid? AccountId = null,
    string? DisplayName = null,
    string? Provider = null,
    bool? IsNewAccount = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static VerifyOtpResult Success(
        Guid accountId,
        string displayName,
        string provider,
        bool isNewAccount)
    {
        return new VerifyOtpResult(
            IsSuccess: true,
            AccountId: accountId,
            DisplayName: displayName,
            Provider: provider,
            IsNewAccount: isNewAccount);
    }

    public static VerifyOtpResult Failure(string errorCode, string errorMessage)
    {
        return new VerifyOtpResult(
            IsSuccess: false,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
    }
}
