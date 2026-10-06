using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E04;

public sealed class BillingWebhookInboxConfiguration : IEntityTypeConfiguration<BillingWebhookInbox>
{
    public void Configure(EntityTypeBuilder<BillingWebhookInbox> builder)
    {
        builder.ToTable("BillingWebhookInbox");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Provider)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.ProviderEventId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SignatureVerified)
            .IsRequired();

        builder.Property(x => x.PayloadHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.ProviderOccurredAt)
            .HasPrecision(3)
            .IsRequired(false);

        builder.Property(x => x.ReceivedAt)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(x => x.ProcessStatus)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Received");

        builder.Property(x => x.ProcessedAt)
            .HasPrecision(3)
            .IsRequired(false);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.HasIndex(x => new { x.Provider, x.ProviderEventId })
            .IsUnique()
            .HasDatabaseName("UQ_BillingWebhookInbox_Provider_ProviderEventId");

        builder.HasIndex(x => x.ProcessStatus)
            .HasDatabaseName("IX_BillingWebhookInbox_ProcessStatus");
    }
}
