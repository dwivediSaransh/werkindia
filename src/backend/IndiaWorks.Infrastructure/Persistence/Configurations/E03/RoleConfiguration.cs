using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E03;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E03;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(x => x.CanonicalName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.JobFamilyId)
            .IsRequired();

        builder.Property(x => x.TaxonomyVersionId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.ValidFrom)
            .IsRequired()
            .HasPrecision(3);

        builder.Property(x => x.ValidTo)
            .IsRequired(false)
            .HasPrecision(3);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasPrecision(3);

        builder.HasOne(x => x.JobFamily)
            .WithMany()
            .HasForeignKey(x => x.JobFamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TaxonomyVersion)
            .WithMany()
            .HasForeignKey(x => x.TaxonomyVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TaxonomyVersionId, x.Slug })
            .HasDatabaseName("UQ_Roles_TaxVersion_Slug")
            .IsUnique();

        builder.HasIndex(x => x.JobFamilyId)
            .HasDatabaseName("IX_Roles_JobFamilyId");

        builder.HasIndex(x => x.TaxonomyVersionId)
            .HasDatabaseName("IX_Roles_TaxonomyVersionId");

        builder.HasIndex(x => x.CanonicalName)
            .HasDatabaseName("IX_Roles_CanonicalName");
    }
}
