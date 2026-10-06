using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E01;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E01;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UserId)
            .IsRequired();
        builder.Property(x => x.DeviceId)
            .IsRequired();
        builder.Property(x => x.ActivePersona)
            .IsRequired();
        builder.Property(x => x.ExpiresOn)
            .IsRequired();
        builder.Property(x => x.CreatedOn)
            .IsRequired();
        builder.Property(x => x.LastActivityOn)
            .IsRequired();
        builder.Property(x => x.IsRevoked)
            .IsRequired();

        // FK to UserAccount with cascade delete
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to UserDevice (restrictive delete - don't cascade delete sessions)
        builder.HasOne<UserDevice>()
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for looking up sessions by user
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserSessions_UserId");

        // Unique index: one active session per user + device
        builder.HasIndex(x => new { x.UserId, x.DeviceId })
            .IsUnique()
            .HasDatabaseName("UX_UserSessions_UserId_DeviceId");

        // Index for expired session cleanup
        builder.HasIndex(x => x.ExpiresOn)
            .HasDatabaseName("IX_UserSessions_ExpiresOn");

        // Index for revoked sessions
        builder.HasIndex(x => x.IsRevoked)
            .HasDatabaseName("IX_UserSessions_IsRevoked");

        // Index for active sessions lookup
        builder.HasIndex(x => new { x.UserId, x.IsRevoked, x.ExpiresOn })
            .HasDatabaseName("IX_UserSessions_UserId_Active");
    }
}
