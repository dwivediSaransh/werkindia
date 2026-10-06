using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E03;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E03;

internal sealed class CredentialTypeConfiguration : IEntityTypeConfiguration<CredentialType>
{
    public void Configure(EntityTypeBuilder<CredentialType> builder)
    {
        builder.ToTable("CredentialTypes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(x => x.TaxonomyVersionId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasPrecision(3);

        builder.HasOne(x => x.TaxonomyVersion)
            .WithMany()
            .HasForeignKey(x => x.TaxonomyVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TaxonomyVersionId, x.Code })
            .HasDatabaseName("UQ_CredentialTypes_TaxVersion_Code")
            .IsUnique();

        builder.HasIndex(x => x.TaxonomyVersionId)
            .HasDatabaseName("IX_CredentialTypes_TaxonomyVersionId");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_CredentialTypes_Name");
    }
}
