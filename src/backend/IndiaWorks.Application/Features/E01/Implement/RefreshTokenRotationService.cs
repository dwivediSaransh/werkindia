using System.Security.Cryptography;
using IndiaWorks.Application.Features.E01.Create;
using IndiaWorks.Domain.E01;
using Microsoft.Extensions.Logging;

namespace IndiaWorks.Application.Features.E01.Implement;

/// <summary>
/// Rotates one-time refresh tokens: validates the presented refresh token hash,
/// detects reuse/revocation, issues a new refresh token hash, updates LastUsedAt,
/// and returns a new short-lived access token.
/// </summary>
public sealed class RefreshTokenRotationService(
    IUserDeviceRepository deviceRepo,
    IUserSessionRepository sessionRepo,
    IJwtTokenService jwtTokenService,
    ILogger<RefreshTokenRotationService> logger)
{
    public async Task<RefreshTokenRotationResult> RotateAsync(
        RefreshTokenRotationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RefreshTokenHash);

        logger.LogDebug(
            "Refresh token rotation requested for user {UserId} device {DeviceId}",
            MaskGuid(request.UserId),
            MaskGuid(request.DeviceId));

        // 1. Find the device by the presented refresh token hash
        var device = await deviceRepo.GetByRefreshTokenHashAsync(request.RefreshTokenHash, cancellationToken);

        if (device == null)
        {
            // Stale or never-valid token - reuse detection
            logger.LogWarning(
                "Refresh token rotation failed: token hash not found for user {UserId}",
                MaskGuid(request.UserId));
            return RefreshTokenRotationResult.Fail(
                "TOKEN_REUSE_DETECTED",
                "The refresh token has been invalidated. Please re-authenticate.");
        }

        // 2. Verify the device belongs to the requesting user
        if (device.UserId != request.UserId)
        {
            logger.LogWarning(
                "Refresh token rotation failed: device {DeviceId} does not belong to user {UserId}",
                MaskGuid(device.Id),
                MaskGuid(request.UserId));
            return RefreshTokenRotationResult.Fail(
                "FORBIDDEN",
                "The refresh token does not belong to this user.");
        }

        // 3. Check if device is revoked
        if (device.IsRevoked)
        {
            logger.LogWarning(
                "Refresh token rotation failed: device {DeviceId} is revoked",
                MaskGuid(device.Id));
            return RefreshTokenRotationResult.Fail(
                "SESSION_REVOKED",
                "The device has been revoked. Please re-authenticate.");
        }

        // 4. Detect token reuse: if the presented hash doesn't match the current stored hash
        // This handles the case where a rotated token is presented again
        if (device.RefreshTokenHash != request.RefreshTokenHash)
        {
            logger.LogWarning(
                "Refresh token reuse detected for user {UserId} device {DeviceId}",
                MaskGuid(request.UserId),
                MaskGuid(device.Id));

            // Revoke all sessions for this user as a security measure
            await RevokeAllUserSessionsAsync(request.UserId, cancellationToken);

            return RefreshTokenRotationResult.Fail(
                "TOKEN_REUSE_DETECTED",
                "Token reuse detected. All sessions have been revoked. Please re-authenticate.");
        }

        // 5. Find the active session for this user+device
        var session = await sessionRepo.GetActiveByUserAndDeviceAsync(
            request.UserId,
            request.DeviceId,
            cancellationToken);

        if (session == null)
        {
            logger.LogWarning(
                "Refresh token rotation failed: no session found for user {UserId} device {DeviceId}",
                MaskGuid(request.UserId),
                MaskGuid(request.DeviceId));
            return RefreshTokenRotationResult.Fail(
                "SESSION_REVOKED",
                "No active session found. Please re-authenticate.");
        }

        // 6. Check if session is revoked
        if (session.IsRevoked)
        {
            logger.LogWarning(
                "Refresh token rotation failed: session {SessionId} is revoked",
                MaskGuid(session.Id));
            return RefreshTokenRotationResult.Fail(
                "SESSION_REVOKED",
                "The session has been revoked. Please re-authenticate.");
        }

        // 7. Check if session is expired
        if (session.IsExpired)
        {
            logger.LogWarning(
                "Refresh token rotation failed: session {SessionId} is expired",
                MaskGuid(session.Id));
            return RefreshTokenRotationResult.Fail(
                "SESSION_REVOKED",
                "The session has expired. Please re-authenticate.");
        }

        // 8. Generate a new refresh token hash
        var newRefreshTokenHash = GenerateRefreshTokenHash();

        // 9. Rotate: update device with new hash and last seen
        device.UpdateRefreshTokenHash(newRefreshTokenHash);
        device.UpdateLastSeen();

        // 10. Update session last activity
        session.Touch();

        // 11. Persist changes
        await deviceRepo.SaveChangesAsync(cancellationToken);
        await sessionRepo.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Refresh token rotated successfully for user {UserId} device {DeviceId}",
            MaskGuid(request.UserId),
            MaskGuid(request.DeviceId));

        // 12. Issue new access token
        var accessTokenResult = jwtTokenService.CreateAccessToken(new CreateAccessTokenRequest(
            Subject: request.UserId,
            SessionId: session.Id,
            Persona: session.ActivePersona.ToString(),
            CancellationToken: cancellationToken));

        if (!accessTokenResult.IsSuccess)
        {
            logger.LogError(
                "Failed to create access token after refresh rotation for user {UserId}: {ErrorCode}",
                MaskGuid(request.UserId),
                accessTokenResult.ErrorCode);
            return RefreshTokenRotationResult.Fail(
                "UNAUTHENTICATED",
                "Failed to issue new access token.");
        }

        // 13. Return new access token and new refresh token hash
        return RefreshTokenRotationResult.Ok(
            accessTokenResult.AccessToken!,
            newRefreshTokenHash);
    }

    private async Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sessions = await sessionRepo.GetAllByUserIdAsync(userId, cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke();
        }

        await sessionRepo.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "All sessions revoked for user {UserId} due to token reuse detection",
            MaskGuid(userId));
    }

    private static string GenerateRefreshTokenHash()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes);
    }

    private static string MaskGuid(Guid guid) => guid.ToString("N")[..8] + "…";
}
