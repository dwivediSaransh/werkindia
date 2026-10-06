using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E03;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E03;

internal sealed class WorkContextConfiguration : IEntityTypeConfiguration<WorkContext>
{
    public void Configure(EntityTypeBuilder<WorkContext> builder)
    {
        builder.ToTable("WorkContexts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(x => x.RoleId)
            .IsRequired();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

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

        builder.HasIndex(x => new { x.RoleId, x.Code })
            .HasDatabaseName("UQ_WorkContexts_Role_Code")
            .IsUnique();

        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("IX_WorkContexts_RoleId");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_WorkContexts_Name");
    }
}
