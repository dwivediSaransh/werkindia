namespace IndiaWorks.Contracts.E04;

/// <summary>
/// Request to receive a payment webhook from a provider.
/// </summary>
public sealed record PaymentWebhookRequest(
    string Provider,
    string ProviderEventId,
    string EventType,
    string Payload,
    string Signature);

/// <summary>
/// Response from processing a payment webhook.
/// </summary>
public sealed record PaymentWebhookResponse(
    bool Success,
    string? InboxId,
    string? ErrorCode,
    string? ErrorMessage);

/// <summary>
/// Error response for payment webhook endpoint.
/// </summary>
public sealed record PaymentWebhookErrorResponse(
    string Code,
    string Message);