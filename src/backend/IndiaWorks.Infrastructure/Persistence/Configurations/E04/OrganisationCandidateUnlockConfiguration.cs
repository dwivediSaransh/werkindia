using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E04;

public sealed class OrganisationCandidateUnlockConfiguration : IEntityTypeConfiguration<OrganisationCandidateUnlock>
{
    public void Configure(EntityTypeBuilder<OrganisationCandidateUnlock> builder)
    {
        builder.ToTable("OrganisationCandidateUnlocks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.CandidateProfileId)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.UnlockedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.OrganisationId, x.CandidateProfileId })
            .IsUnique()
            .HasDatabaseName("IX_OrganisationCandidateUnlocks_OrgId_CandidateProfileId");
    }
}