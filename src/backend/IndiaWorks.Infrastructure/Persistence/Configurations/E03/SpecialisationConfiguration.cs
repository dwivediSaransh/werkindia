using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E03;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E03;

internal sealed class SpecialisationConfiguration : IEntityTypeConfiguration<Specialisation>
{
    public void Configure(EntityTypeBuilder<Specialisation> builder)
    {
        builder.ToTable("Specialisations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(x => x.RoleId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.ParentId)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasPrecision(3);

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RoleId, x.Code })
            .HasDatabaseName("UQ_Specialisations_Role_Code")
            .IsUnique();

        builder.HasIndex(x => x.ParentId)
            .HasDatabaseName("IX_Specialisations_ParentId");

        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("IX_Specialisations_RoleId");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_Specialisations_Name");
    }
}
