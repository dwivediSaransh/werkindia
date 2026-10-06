using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Infrastructure.Persistence.Configurations.E04;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.BillingInvoiceId)
            .IsRequired(false);

        builder.Property(x => x.Provider)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.ProviderTransactionId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.TransactionType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.FailureCode)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.FailureMessage)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.OccurredAt)
            .HasPrecision(3)
            .IsRequired();

        builder.HasIndex(x => new { x.Provider, x.ProviderTransactionId })
            .IsUnique()
            .HasDatabaseName("UQ_PaymentTransactions_Provider_ProviderTransactionId");

        builder.HasIndex(x => x.OrganisationId)
            .HasDatabaseName("IX_PaymentTransactions_OrganisationId");

        builder.HasIndex(x => x.BillingInvoiceId)
            .HasDatabaseName("IX_PaymentTransactions_BillingInvoiceId");
    }
}