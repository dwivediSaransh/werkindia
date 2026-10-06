using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E04;

public sealed class SubscriptionPlanVersionConfiguration : IEntityTypeConfiguration<SubscriptionPlanVersion>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanVersion> builder)
    {
        builder.ToTable("SubscriptionPlanVersions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.PlanId)
            .IsRequired();

        builder.Property(x => x.Interval)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(x => x.TrialDays)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.PlanId)
            .HasDatabaseName("IX_SubscriptionPlanVersions_PlanId");

        builder.HasIndex(x => new { x.PlanId, x.Interval })
            .IsUnique()
            .HasDatabaseName("IX_SubscriptionPlanVersions_PlanId_Interval");
    }
}