namespace IndiaWorks.Application.Features.E01.Create;

public sealed record CreateAccessTokenRequest(
    Guid Subject,
    Guid SessionId,
    string Persona,
    CancellationToken CancellationToken = default);

public sealed record AccessTokenResult(
    bool IsSuccess,
    string? AccessToken = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static AccessTokenResult Ok(string accessToken) => new(true, accessToken);
    public static AccessTokenResult Fail(string errorCode, string errorMessage) => new(false, ErrorCode: errorCode, ErrorMessage: errorMessage);
}

public sealed record JwtSettings(
    string Issuer,
    string Audience,
    int AccessTokenSeconds = 900,
    int RefreshTokenSeconds = 604800)
{
    public TimeSpan AccessTokenLifetime => TimeSpan.FromSeconds(AccessTokenSeconds);
}
