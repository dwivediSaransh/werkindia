using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E04;

public sealed class OrganisationSubscriptionConfiguration : IEntityTypeConfiguration<OrganisationSubscription>
{
    public void Configure(EntityTypeBuilder<OrganisationSubscription> builder)
    {
        builder.ToTable("OrganisationSubscriptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.PlanId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CurrentPeriodStart)
            .IsRequired();

        builder.Property(x => x.CurrentPeriodEnd)
            .IsRequired();

        builder.Property(x => x.CancelAtPeriodEnd)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(x => x.OrganisationId)
            .IsUnique()
            .HasDatabaseName("IX_OrganisationSubscriptions_OrganisationId");

        builder.HasIndex(x => x.PlanId)
            .HasDatabaseName("IX_OrganisationSubscriptions_PlanId");
    }
}