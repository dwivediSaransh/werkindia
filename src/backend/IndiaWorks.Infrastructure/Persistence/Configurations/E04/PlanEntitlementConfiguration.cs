using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E04;

public sealed class PlanEntitlementConfiguration : IEntityTypeConfiguration<PlanEntitlement>
{
    public void Configure(EntityTypeBuilder<PlanEntitlement> builder)
    {
        builder.ToTable("PlanEntitlements");
        builder.HasKey(x => new { x.PlanVersionId, x.EntitlementDefinitionCode });

        builder.Property(x => x.PlanVersionId)
            .IsRequired();

        builder.Property(x => x.EntitlementDefinitionCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Enabled)
            .IsRequired();

        builder.Property(x => x.Limit)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.HasOne<SubscriptionPlanVersion>()
            .WithMany()
            .HasForeignKey(x => x.PlanVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EntitlementDefinition>()
            .WithMany()
            .HasForeignKey(x => x.EntitlementDefinitionCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}