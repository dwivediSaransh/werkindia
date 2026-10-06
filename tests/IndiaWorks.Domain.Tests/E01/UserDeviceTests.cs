using IndiaWorks.Domain.E01;

namespace IndiaWorks.Domain.Tests.E01;

public sealed class UserDeviceTests
{
    [Fact]
    public void Create_succeeds_with_valid_properties()
    {
        var userId = Guid.NewGuid();
        var deviceId = "device-001";
        var refreshTokenHash = "hashed-refresh-token";

        var device = UserDevice.Create(userId, deviceId, refreshTokenHash, "Chrome on Windows", "Windows", "Chrome");

        Assert.NotEqual(Guid.Empty, device.Id);
        Assert.Equal(userId, device.UserId);
        Assert.Equal(deviceId, device.DeviceId);
        Assert.Equal(refreshTokenHash, device.RefreshTokenHash);
        Assert.Equal("Chrome on Windows", device.DeviceName);
        Assert.Equal("Windows", device.Platform);
        Assert.Equal("Chrome", device.Browser);
        Assert.False(device.IsRevoked);
        Assert.True(device.CreatedOn > default(DateTime));
        Assert.True(device.LastSeenOn > default(DateTime));
    }

    [Fact]
    public void Create_defaults_optional_properties_to_null()
    {
        var userId = Guid.NewGuid();
        var deviceId = "device-002";
        var refreshTokenHash = "hashed-refresh-token";

        var device = UserDevice.Create(userId, deviceId, refreshTokenHash);

        Assert.Null(device.DeviceName);
        Assert.Null(device.Platform);
        Assert.Null(device.Browser);
    }

    [Fact]
    public void Create_throws_for_blank_device_id()
    {
        var ex = Assert.Throws<ArgumentException>(() => UserDevice.Create(Guid.NewGuid(), "", "hash"));
        Assert.Equal("deviceId", ex.ParamName);
    }

    [Fact]
    public void Create_throws_for_null_device_id()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => UserDevice.Create(Guid.NewGuid(), null!, "hash"));
        Assert.Equal("deviceId", ex.ParamName);
    }

    [Fact]
    public void Create_throws_for_blank_refresh_token_hash()
    {
        var ex = Assert.Throws<ArgumentException>(() => UserDevice.Create(Guid.NewGuid(), "device-id", ""));
        Assert.Equal("refreshTokenHash", ex.ParamName);
    }

    [Fact]
    public void Create_throws_for_null_refresh_token_hash()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => UserDevice.Create(Guid.NewGuid(), "device-id", null!));
        Assert.Equal("refreshTokenHash", ex.ParamName);
    }

    [Fact]
    public void UpdateLastSeen_updates_timestamp()
    {
        var device = UserDevice.Create(Guid.NewGuid(), "device-001", "hash");
        var before = device.LastSeenOn;
        Thread.Sleep(10);
        device.UpdateLastSeen();

        Assert.True(device.LastSeenOn > before);
    }

    [Fact]
    public void Revoke_marks_device_as_revoked()
    {
        var device = UserDevice.Create(Guid.NewGuid(), "device-001", "hash");
        Assert.False(device.IsRevoked);

        device.Revoke();

        Assert.True(device.IsRevoked);
    }

    [Fact]
    public void UpdateRefreshTokenHash_changes_hash()
    {
        var device = UserDevice.Create(Guid.NewGuid(), "device-001", "old-hash");
        device.UpdateRefreshTokenHash("new-hash");

        Assert.Equal("new-hash", device.RefreshTokenHash);
    }

    [Fact]
    public void UpdateRefreshTokenHash_throws_for_blank_hash()
    {
        var device = UserDevice.Create(Guid.NewGuid(), "device-001", "hash");
        var ex = Assert.Throws<ArgumentException>(() => device.UpdateRefreshTokenHash(""));
        Assert.Equal("newRefreshTokenHash", ex.ParamName);
    }

    [Fact]
    public void UpdateRefreshTokenHash_throws_for_null_hash()
    {
        var device = UserDevice.Create(Guid.NewGuid(), "device-001", "hash");
        var ex = Assert.Throws<ArgumentNullException>(() => device.UpdateRefreshTokenHash(null!));
        Assert.Equal("newRefreshTokenHash", ex.ParamName);
    }
}
