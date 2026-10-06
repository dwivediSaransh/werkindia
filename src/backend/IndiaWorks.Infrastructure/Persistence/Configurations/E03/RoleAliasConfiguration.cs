using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E03;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E03;

internal sealed class RoleAliasConfiguration : IEntityTypeConfiguration<RoleAlias>
{
    public void Configure(EntityTypeBuilder<RoleAlias> builder)
    {
        builder.ToTable("RoleAliases");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(x => x.RoleId)
            .IsRequired();

        builder.Property(x => x.Alias)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.LanguageCode)
            .IsRequired(false)
            .HasMaxLength(10);

        builder.Property(x => x.RegionCode)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(x => x.AliasType)
            .IsRequired(false)
            .HasMaxLength(30);

        builder.Property(x => x.Weight)
            .HasPrecision(6, 3)
            .IsRequired(false);

        builder.Property(x => x.IsDeprecated)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasPrecision(3);

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.RoleId, x.Alias })
            .HasDatabaseName("UQ_RoleAliases_Role_Alias")
            .IsUnique();

        builder.HasIndex(x => x.Alias)
            .HasDatabaseName("IX_RoleAliases_Alias")
            .IncludeProperties(x => new { x.RoleId, x.Weight });
    }
}
