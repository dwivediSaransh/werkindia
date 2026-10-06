using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E03;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E03;

internal sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills");
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

        builder.Property(x => x.ParentId)
            .IsRequired(false);

        builder.Property(x => x.TaxonomyVersionId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasPrecision(3);

        builder.HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TaxonomyVersion)
            .WithMany()
            .HasForeignKey(x => x.TaxonomyVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TaxonomyVersionId, x.Code })
            .HasDatabaseName("UQ_Skills_TaxVersion_Code")
            .IsUnique();

        builder.HasIndex(x => x.ParentId)
            .HasDatabaseName("IX_Skills_ParentId");

        builder.HasIndex(x => x.TaxonomyVersionId)
            .HasDatabaseName("IX_Skills_TaxonomyVersionId");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_Skills_Name");
    }
}
