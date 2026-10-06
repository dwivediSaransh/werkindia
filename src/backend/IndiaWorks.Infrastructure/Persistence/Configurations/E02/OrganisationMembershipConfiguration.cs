using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E01;
using IndiaWorks.Domain.E02;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E02;

public sealed class OrganisationMembershipConfiguration : IEntityTypeConfiguration<OrganisationMembership>
{
    public void Configure(EntityTypeBuilder<OrganisationMembership> builder)
    {
        builder.ToTable("OrganisationMemberships");
        builder.HasKey(x => new { x.OrganisationId, x.UserAccountId });

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.UserAccountId)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserAccountId)
            .HasDatabaseName("IX_OrganisationMemberships_UserAccountId");

        builder.HasIndex(x => new { x.OrganisationId, x.Status })
            .HasDatabaseName("IX_OrganisationMemberships_OrganisationId_Status");
    }
}
