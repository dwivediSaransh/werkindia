using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E01;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E01;

public sealed class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.ToTable("UserDevices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UserId)
            .IsRequired();
        builder.Property(x => x.DeviceId)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(x => x.DeviceName)
            .IsRequired(false)
            .HasMaxLength(200);
        builder.Property(x => x.Platform)
            .IsRequired(false)
            .HasMaxLength(100);
        builder.Property(x => x.Browser)
            .IsRequired(false)
            .HasMaxLength(100);
        builder.Property(x => x.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(x => x.CreatedOn)
            .IsRequired();
        builder.Property(x => x.LastSeenOn)
            .IsRequired();
        builder.Property(x => x.IsRevoked)
            .IsRequired();

        // FK to UserAccount with cascade delete
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for looking up devices by user
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserDevices_UserId");

        // Unique index: one device per user per device ID
        builder.HasIndex(x => new { x.UserId, x.DeviceId })
            .IsUnique()
            .HasDatabaseName("UX_UserDevices_UserId_DeviceId");

        // Index for revoked/active device queries
        builder.HasIndex(x => x.IsRevoked)
            .HasDatabaseName("IX_UserDevices_IsRevoked");

        // Index for cleanup of old refresh token hashes
        builder.HasIndex(x => x.LastSeenOn)
            .HasDatabaseName("IX_UserDevices_LastSeenOn");

        // Unique index: refresh-token hash must be unique across devices
        builder.HasIndex(x => x.RefreshTokenHash)
            .IsUnique()
            .HasDatabaseName("UX_UserDevices_RefreshTokenHash");
    }
}
