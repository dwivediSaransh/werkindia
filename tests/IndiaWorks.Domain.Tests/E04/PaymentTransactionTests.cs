using IndiaWorks.Domain.E04;
using Xunit;

namespace IndiaWorks.Domain.Tests.E04;

public sealed class PaymentTransactionTests
{
    [Fact]
    public void Create_WithValidParameters_CapturesPaymentTransaction()
    {
        var organisationId = Guid.NewGuid();
        var billingInvoiceId = 12345L;
        var provider = "Stripe";
        var providerTransactionId = "txn_1234567890";
        var transactionType = "PAYMENT";
        var status = "Succeeded";
        var currency = "INR";
        var amount = 999.50m;
        var failureCode = (string?)null;
        var failureMessage = (string?)null;
        var occurredAt = DateTime.UtcNow.AddMinutes(-10);

        var transaction = PaymentTransaction.Create(
            organisationId,
            billingInvoiceId,
            provider,
            providerTransactionId,
            transactionType,
            status,
            currency,
            amount,
            failureCode,
            failureMessage,
            occurredAt);

        Assert.Equal(0, transaction.Id); // Database-generated
        Assert.Equal(organisationId, transaction.OrganisationId);
        Assert.Equal(billingInvoiceId, transaction.BillingInvoiceId);
        Assert.Equal(provider, transaction.Provider);
        Assert.Equal(providerTransactionId, transaction.ProviderTransactionId);
        Assert.Equal(transactionType, transaction.TransactionType);
        Assert.Equal(status, transaction.Status);
        Assert.Equal(currency, transaction.Currency);
        Assert.Equal(amount, transaction.Amount);
        Assert.Null(transaction.FailureCode);
        Assert.Null(transaction.FailureMessage);
        Assert.Equal(occurredAt, transaction.OccurredAt);
    }

    [Fact]
    public void Create_WithNullBillingInvoiceId_Works()
    {
        var transaction = PaymentTransaction.Create(
            Guid.NewGuid(),
            null,
            "Stripe",
            "txn_1234567890",
            "PAYMENT",
            "Succeeded",
            "INR",
            999.50m,
            null,
            null,
            DateTime.UtcNow);

        Assert.Null(transaction.BillingInvoiceId);
    }

    [Fact]
    public void Create_WithFailureInfo_CapturesFailureDetails()
    {
        var transaction = PaymentTransaction.Create(
            Guid.NewGuid(),
            12345L,
            "Stripe",
            "txn_1234567890",
            "PAYMENT",
            "Failed",
            "INR",
            999.50m,
            "card_declined",
            "Your card was declined.",
            DateTime.UtcNow);

        Assert.Equal("Failed", transaction.Status);
        Assert.Equal("card_declined", transaction.FailureCode);
        Assert.Equal("Your card was declined.", transaction.FailureMessage);
    }

    [Fact]
    public void Create_WithEmptyOrganisationId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateValid(organisationId: Guid.Empty));
        Assert.Equal("organisationId", ex.ParamName);
    }

    [Fact]
    public void Create_WithEmptyProvider_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(provider: " "));
    }

    [Fact]
    public void Create_WithEmptyProviderTransactionId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(providerTransactionId: " "));
    }

    [Fact]
    public void Create_WithEmptyTransactionType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(transactionType: " "));
    }

    [Fact]
    public void Create_WithEmptyStatus_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(status: " "));
    }

    [Fact]
    public void Create_WithEmptyCurrency_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(currency: " "));
    }

    [Fact]
    public void Create_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateValid(amount: 0));
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateValid(amount: -1));
    }

    [Fact]
    public void UpdateStatus_UpdatesStatusAndFailureInfo()
    {
        var transaction = CreateValid();

        transaction.UpdateStatus("Failed", "insufficient_funds", "Insufficient funds");

        Assert.Equal("Failed", transaction.Status);
        Assert.Equal("insufficient_funds", transaction.FailureCode);
        Assert.Equal("Insufficient funds", transaction.FailureMessage);
    }

    [Fact]
    public void UpdateStatus_WithEmptyStatus_ThrowsArgumentException()
    {
        var transaction = CreateValid();

        Assert.Throws<ArgumentException>(() => transaction.UpdateStatus(" "));
    }

    [Fact]
    public void UpdateStatus_WithEmptyFailureCode_ClearsFailureCode()
    {
        var transaction = CreateValid();
        // First set failure info
        transaction.UpdateStatus("Failed", "code", "message");

        // Then update without failure info
        transaction.UpdateStatus("Succeeded");

        Assert.Equal("Succeeded", transaction.Status);
        Assert.Null(transaction.FailureCode);
        Assert.Null(transaction.FailureMessage);
    }

    [Fact]
    public void UpdateStatus_WithNullFailureMessage_ClearsFailureMessage()
    {
        var transaction = CreateValid();
        transaction.UpdateStatus("Failed", "code", "message");

        transaction.UpdateStatus("Failed", "code", null);

        Assert.Equal("Failed", transaction.Status);
        Assert.Equal("code", transaction.FailureCode);
        Assert.Null(transaction.FailureMessage);
    }

    private static PaymentTransaction CreateValid(
        Guid? organisationId = null,
        long? billingInvoiceId = 12345L,
        string provider = "Stripe",
        string providerTransactionId = "txn_1234567890",
        string transactionType = "PAYMENT",
        string status = "Succeeded",
        string currency = "INR",
        decimal amount = 999.50m,
        string? failureCode = null,
        string? failureMessage = null,
        DateTime? occurredAt = null)
    {
        return PaymentTransaction.Create(
            organisationId ?? Guid.NewGuid(),
            billingInvoiceId,
            provider,
            providerTransactionId,
            transactionType,
            status,
            currency,
            amount,
            failureCode,
            failureMessage,
            occurredAt ?? DateTime.UtcNow.AddMinutes(-10));
    }
}