namespace IndiaWorks.Domain.E01;

/// <summary>
/// Represents a device (browser, mobile app, etc.) that a user has used to access
/// IndiaWorks. Stores device metadata, refresh-token hash, and revocation status.
/// </summary>
public sealed class UserDevice
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DeviceId { get; private set; } = default!;
    public string? DeviceName { get; private set; }
    public string? Platform { get; private set; }
    public string? Browser { get; private set; }
    public string RefreshTokenHash { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public DateTime LastSeenOn { get; private set; }
    public bool IsRevoked { get; private set; }

    private UserDevice()
    {
    }

    public static UserDevice Create(
        Guid userId,
        string deviceId,
        string refreshTokenHash,
        string? deviceName = null,
        string? platform = null,
        string? browser = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenHash);

        return new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceId = deviceId,
            DeviceName = deviceName,
            Platform = platform,
            Browser = browser,
            RefreshTokenHash = refreshTokenHash,
            CreatedOn = DateTime.UtcNow,
            LastSeenOn = DateTime.UtcNow,
            IsRevoked = false
        };
    }

    public void UpdateLastSeen()
    {
        LastSeenOn = DateTime.UtcNow;
    }

    public void Revoke()
    {
        IsRevoked = true;
    }

    public void UpdateRefreshTokenHash(string newRefreshTokenHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newRefreshTokenHash);
        RefreshTokenHash = newRefreshTokenHash;
    }
}
