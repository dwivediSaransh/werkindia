namespace IndiaWorks.Application.Features.E01.Implement;

public sealed record RefreshTokenRotationRequest(
    Guid UserId,
    Guid DeviceId,
    string RefreshTokenHash,
    CancellationToken CancellationToken = default);

public sealed record RefreshTokenRotationResult(
    bool IsSuccess,
    string? AccessToken = null,
    string? NewRefreshTokenHash = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static RefreshTokenRotationResult Ok(string accessToken, string newRefreshTokenHash) =>
        new(true, accessToken, newRefreshTokenHash);

    public static RefreshTokenRotationResult Fail(string errorCode, string errorMessage) =>
        new(false, ErrorCode: errorCode, ErrorMessage: errorMessage);
}
