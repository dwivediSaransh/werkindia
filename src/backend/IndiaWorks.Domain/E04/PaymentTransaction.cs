namespace IndiaWorks.Domain.E04;

/// <summary>
/// Records a payment transaction from a provider (payment, refund, adjustment).
/// </summary>
public sealed class PaymentTransaction
{
    public long Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public long? BillingInvoiceId { get; private set; }
    public string Provider { get; private set; } = default!;
    public string ProviderTransactionId { get; private set; } = default!;
    public string TransactionType { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public string Currency { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private PaymentTransaction() { }

    public static PaymentTransaction Create(
        Guid organisationId,
        long? billingInvoiceId,
        string provider,
        string providerTransactionId,
        string transactionType,
        string status,
        string currency,
        decimal amount,
        string? failureCode,
        string? failureMessage,
        DateTime occurredAt)
    {
        if (organisationId == Guid.Empty)
            throw new ArgumentException("OrganisationId must not be empty.", nameof(organisationId));

        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        return new PaymentTransaction
        {
            Id = 0, // Database-generated
            OrganisationId = organisationId,
            BillingInvoiceId = billingInvoiceId,
            Provider = provider.Trim(),
            ProviderTransactionId = providerTransactionId.Trim(),
            TransactionType = transactionType.Trim(),
            Status = status.Trim(),
            Currency = currency.Trim(),
            Amount = amount,
            FailureCode = failureCode?.Trim(),
            FailureMessage = failureMessage?.Trim(),
            OccurredAt = occurredAt
        };
    }

    public void UpdateStatus(string status, string? failureCode = null, string? failureMessage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        Status = status.Trim();
        FailureCode = failureCode?.Trim();
        FailureMessage = failureMessage?.Trim();
    }
}