namespace IndiaWorks.Application.Features.E04.Implement;

public sealed record EntitlementEvaluationResult(
    bool IsSuccess,
    string? Decision,
    decimal Limit,
    decimal Used,
    decimal Remaining,
    DateTime? ResetWindowStart,
    DateTime? ResetWindowEnd,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static EntitlementEvaluationResult Success(
        decimal limit,
        decimal used,
        DateTime? resetWindowStart,
        DateTime? resetWindowEnd)
    {
        var remaining = limit - used;
        var decision = used < limit ? "ALLOW" : "METER";

        return new EntitlementEvaluationResult(
            IsSuccess: true,
            Decision: decision,
            Limit: limit,
            Used: used,
            Remaining: remaining,
            ResetWindowStart: resetWindowStart,
            ResetWindowEnd: resetWindowEnd);
    }

    public static EntitlementEvaluationResult Denied(string errorCode, string errorMessage)
    {
        return new EntitlementEvaluationResult(
            IsSuccess: false,
            Decision: "DENY",
            Limit: 0,
            Used: 0,
            Remaining: 0,
            ResetWindowStart: null,
            ResetWindowEnd: null,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
    }
}