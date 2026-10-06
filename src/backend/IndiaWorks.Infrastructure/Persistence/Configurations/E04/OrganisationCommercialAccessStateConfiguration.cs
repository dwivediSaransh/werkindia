using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E04;

public sealed class OrganisationCommercialAccessStateConfiguration : IEntityTypeConfiguration<OrganisationCommercialAccessState>
{
    public void Configure(EntityTypeBuilder<OrganisationCommercialAccessState> builder)
    {
        builder.ToTable("OrganisationCommercialAccessStates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.FreeCandidateUnlockLimit)
            .IsRequired();

        builder.Property(x => x.FreeCandidateUnlockUsed)
            .IsRequired();

        builder.Property(x => x.ProvisioningState)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ResetCadence)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(x => x.OrganisationId)
            .IsUnique()
            .HasDatabaseName("IX_OrganisationCommercialAccessStates_OrganisationId");
    }
}