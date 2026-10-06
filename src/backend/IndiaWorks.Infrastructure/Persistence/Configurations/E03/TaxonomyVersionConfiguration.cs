using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E03;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E03;

internal sealed class TaxonomyVersionConfiguration : IEntityTypeConfiguration<TaxonomyVersion>
{
    public void Configure(EntityTypeBuilder<TaxonomyVersion> builder)
    {
        builder.ToTable("TaxonomyVersions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(x => x.SemanticVersion)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.EffectiveFrom)
            .IsRequired()
            .HasPrecision(3);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.ReleaseNotes)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasPrecision(3);

        builder.HasIndex(x => x.SemanticVersion)
            .HasDatabaseName("UQ_TaxonomyVersions_SemanticVersion")
            .IsUnique();

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_TaxonomyVersions_Status");
    }
}
