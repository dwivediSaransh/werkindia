using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E03;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E03;

public sealed class GeographyConfiguration : IEntityTypeConfiguration<Geography>
{
    public void Configure(EntityTypeBuilder<Geography> builder)
    {
        builder.ToTable("Geographies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ParentId)
            .IsRequired(false);

        builder.Property(x => x.Latitude)
            .HasPrecision(9, 6)
            .IsRequired(false);

        builder.Property(x => x.Longitude)
            .HasPrecision(9, 6)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Parent)
            .AutoInclude(false);

        builder.Navigation(x => x.Children)
            .AutoInclude(false);

        builder.HasIndex(x => x.Code)
            .HasDatabaseName("IX_Geographies_Code")
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL");

        builder.HasIndex(x => new { x.Type, x.Name })
            .HasDatabaseName("IX_Geographies_Type_Name");

        builder.HasIndex(x => x.ParentId)
            .HasDatabaseName("IX_Geographies_ParentId");
    }
}
