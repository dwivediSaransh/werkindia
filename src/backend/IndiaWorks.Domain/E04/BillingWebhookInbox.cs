namespace IndiaWorks.Domain.E04;

/// <summary>
/// Stores incoming billing webhook events from payment providers for idempotent processing.
/// </summary>
public sealed class BillingWebhookInbox
{
    public Guid Id { get; private set; }
    public string Provider { get; private set; } = default!;
    public string ProviderEventId { get; private set; } = default!;
    public string EventType { get; private set; } = default!;
    public bool SignatureVerified { get; private set; }
    public string PayloadHash { get; private set; } = default!;
    public DateTime? ProviderOccurredAt { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public string ProcessStatus { get; private set; } = default!;
    public DateTime? ProcessedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private BillingWebhookInbox() { }

    public static BillingWebhookInbox Create(
        string provider,
        string providerEventId,
        string eventType,
        bool signatureVerified,
        string payloadHash,
        DateTime? providerOccurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadHash);

        return new BillingWebhookInbox
        {
            Id = Guid.NewGuid(),
            Provider = provider.Trim(),
            ProviderEventId = providerEventId.Trim(),
            EventType = eventType.Trim(),
            SignatureVerified = signatureVerified,
            PayloadHash = payloadHash.Trim(),
            ProviderOccurredAt = providerOccurredAt,
            ReceivedAt = DateTime.UtcNow,
            ProcessStatus = "Received",
            ProcessedAt = null,
            ErrorMessage = null
        };
    }

    public void MarkProcessed()
    {
        ProcessStatus = "Processed";
        ProcessedAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string? errorMessage)
    {
        ProcessStatus = "Failed";
        ProcessedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage?.Trim();
    }
}
