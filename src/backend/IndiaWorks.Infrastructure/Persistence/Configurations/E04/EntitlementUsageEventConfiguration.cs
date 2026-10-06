using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E04;

public sealed class EntitlementUsageEventConfiguration : IEntityTypeConfiguration<EntitlementUsageEvent>
{
    public void Configure(EntityTypeBuilder<EntitlementUsageEvent> builder)
    {
        builder.ToTable("EntitlementUsageEvents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.EntitlementDefinitionCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PlanVersionId)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.EventDate)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_EntitlementUsageEvents_IdempotencyKey");

        builder.HasIndex(x => x.OrganisationId)
            .HasDatabaseName("IX_EntitlementUsageEvents_OrganisationId");

        builder.HasIndex(x => x.EntitlementDefinitionCode)
            .HasDatabaseName("IX_EntitlementUsageEvents_EntitlementDefinitionCode");
    }
}