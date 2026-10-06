using IndiaWorks.Domain.E02;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E02;

internal sealed class OrganisationInvitationConfiguration
    : IEntityTypeConfiguration<OrganisationInvitation>
{
    public void Configure(EntityTypeBuilder<OrganisationInvitation> b)
    {
        b.ToTable("OrganisationInvitations");

        b.HasKey(x => x.Id);

        b.Property(x => x.OrganisationId)
            .IsRequired();

        b.Property(x => x.InvitedByUserAccountId)
            .IsRequired();

        b.Property(x => x.InviteeIdentifierType)
            .IsRequired()
            .HasMaxLength(30);

        // SHA-256 = 32 bytes; stored as fixed-length binary.
        b.Property(x => x.InviteeIdentifierHash)
            .IsRequired()
            .HasMaxLength(32)
            .IsFixedLength();

        b.Property(x => x.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        // Secure token hash — 64 bytes (SHA-512 or HMAC-SHA256).
        b.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(64)
            .IsFixedLength();

        b.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(InvitationStatus.Pending);

        b.Property(x => x.ExpiresAt)
            .IsRequired()
            .HasPrecision(3);

        b.Property(x => x.CreatedAt)
            .IsRequired()
            .HasPrecision(3);

        b.Property(x => x.AcceptedByUserAccountId)
            .IsRequired(false);

        b.Property(x => x.AcceptedAt)
            .IsRequired(false)
            .HasPrecision(3);

        b.Property(x => x.RevokedAt)
            .IsRequired(false)
            .HasPrecision(3);

        // FK → Organisation (cascades delete intentionally: delete org removes invitations)
        b.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index: look up open invitations for an org quickly.
        b.HasIndex(x => new { x.OrganisationId, x.Status, x.ExpiresAt })
            .HasDatabaseName("IX_OrganisationInvitations_Org_Status_Expires");

        // Unique index: one active (Pending) token per invitee per org.
        // Partial filter: only enforced when Status = 'Pending'.
        b.HasIndex(x => new { x.OrganisationId, x.InviteeIdentifierHash })
            .HasFilter("[Status] = 'Pending'")
            .IsUnique()
            .HasDatabaseName("UX_OrganisationInvitations_Org_InviteeHash_Pending");
    }
}
