using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E02;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E02;

public sealed class OrganisationIdentifierConfiguration : IEntityTypeConfiguration<OrganisationIdentifier>
{
    public void Configure(EntityTypeBuilder<OrganisationIdentifier> builder)
    {
        builder.ToTable("OrganisationIdentifiers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.IdentifierType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.IdentifierValueEncrypted)
            .IsRequired(false);

        builder.Property(x => x.IdentifierHash)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.MaskedDisplay)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.VerificationSource)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.VerifiedAt)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.IdentifierType, x.IdentifierHash })
            .IsUnique()
            .HasDatabaseName("UQ_OrganisationIdentifiers_Type_Hash");

        builder.HasIndex(x => x.OrganisationId)
            .HasDatabaseName("IX_OrganisationIdentifiers_OrganisationId");
    }
}
