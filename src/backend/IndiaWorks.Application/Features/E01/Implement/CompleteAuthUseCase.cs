using IndiaWorks.Application.Features.E01.Create;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Domain.E01;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace IndiaWorks.Application.Features.E01.Implement;

/// <summary>
/// Completes the auth flow after OTP verification: creates a device and session,
/// then issues a JWT access token. Called by the /verify endpoint.
/// </summary>
public sealed class CompleteAuthUseCase(
    IUserDeviceRepository deviceRepo,
    IUserSessionRepository sessionRepo,
    IJwtTokenService jwtTokenService,
    ILogger<CompleteAuthUseCase> logger)
{
    private static readonly TimeSpan DefaultSessionDuration = TimeSpan.FromDays(7);

    public async Task<CompleteAuthResult> ExecuteAsync(
        Guid accountId,
        string displayName,
        string provider,
        bool isNewAccount,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        // Generate a device id if not supplied
        var resolvedDeviceId = deviceId ?? Guid.NewGuid().ToString("N");

        logger.LogDebug(
            "Completing auth for account {AccountId}, device {DeviceId}",
            MaskGuid(accountId),
            MaskGuid(Guid.Parse(resolvedDeviceId)));

        // 1. Create a refresh token hash for the device
        var refreshTokenHash = GenerateRefreshTokenHash();

        // 2. Create the device
        var device = UserDevice.Create(
            userId: accountId,
            deviceId: resolvedDeviceId,
            refreshTokenHash: refreshTokenHash,
            deviceName: null,
            platform: null,
            browser: null);

        await deviceRepo.AddAsync(device, cancellationToken);

        // 3. Create the session for the created device
        var session = UserSession.Create(
            userId: accountId,
            deviceId: device.Id,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.Add(DefaultSessionDuration));

        await sessionRepo.AddAsync(session, cancellationToken);

        // 4. Issue access token
        var accessTokenResult = jwtTokenService.CreateAccessToken(new CreateAccessTokenRequest(
            Subject: accountId,
            SessionId: session.Id,
            Persona: session.ActivePersona.ToString(),
            CancellationToken: cancellationToken));

        if (!accessTokenResult.IsSuccess)
        {
            logger.LogError(
                "Failed to issue access token after auth completion for account {AccountId}: {ErrorCode}",
                MaskGuid(accountId),
                accessTokenResult.ErrorCode);

            return CompleteAuthResult.Fail(
                accessTokenResult.ErrorCode!,
                accessTokenResult.ErrorMessage!,
                device.Id,
                session.Id);
        }

        logger.LogInformation(
            "Auth completed for account {AccountId}, session {SessionId}",
            MaskGuid(accountId),
            MaskGuid(session.Id));

        return CompleteAuthResult.Ok(
            accountId: accountId,
            displayName: displayName,
            provider: provider,
            isNewAccount: isNewAccount,
            accessToken: accessTokenResult.AccessToken!,
            expiresOn: DateTime.UtcNow.Add(ProviderServices.JwtSettings.AccessTokenLifetime),
            deviceId: device.Id,
            refreshTokenHash: refreshTokenHash,
            sessionId: session.Id);
    }

    private static string GenerateRefreshTokenHash()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes);
    }

    private static string MaskGuid(Guid guid) => guid.ToString("N")[..8] + "…";

    private static class ProviderServices
    {
        public static readonly JwtSettings JwtSettings = new(
            Issuer: "IndiaWorks",
            Audience: "IndiaWorks",
            AccessTokenSeconds: 900);
    }
}

public sealed record CompleteAuthResult(
    bool IsSuccess,
    Guid? AccountId = null,
    string? DisplayName = null,
    string? Provider = null,
    bool? IsNewAccount = null,
    string? AccessToken = null,
    DateTime? ExpiresOn = null,
    Guid? DeviceId = null,
    string? RefreshTokenHash = null,
    Guid? SessionId = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static CompleteAuthResult Ok(
        Guid accountId,
        string displayName,
        string provider,
        bool isNewAccount,
        string accessToken,
        DateTime expiresOn,
        Guid deviceId,
        string refreshTokenHash,
        Guid sessionId) =>
        new(
            IsSuccess: true,
            AccountId: accountId,
            DisplayName: displayName,
            Provider: provider,
            IsNewAccount: isNewAccount,
            AccessToken: accessToken,
            ExpiresOn: expiresOn,
            DeviceId: deviceId,
            RefreshTokenHash: refreshTokenHash,
            SessionId: sessionId);

    public static CompleteAuthResult Fail(
        string errorCode,
        string errorMessage,
        Guid deviceId,
        Guid sessionId) =>
        new(
            IsSuccess: false,
            DeviceId: deviceId,
            SessionId: sessionId,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
}
