using System.Threading;
using System.Threading.Tasks;
using IndiaWorks.Application.Features.E04.Create;
using IndiaWorks.Application.Features.E04.Create.Dtos;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Application.Features.E04.Receive;

/// <summary>
/// Handler for receiving and processing payment webhooks with idempotency.
/// </summary>
public sealed class ReceivePaymentWebhookHandler(
    IBillingWebhookInboxRepository inboxRepository,
    IPaymentGateway paymentGateway)
{
    public async Task<ReceivePaymentWebhookResult> HandleAsync(
        ReceivePaymentWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check for duplicate using provider event id (idempotency key)
        var existingInbox = await inboxRepository.GetByProviderAndEventIdAsync(
            request.Provider,
            request.ProviderEventId,
            cancellationToken);

        if (existingInbox != null)
        {
            // Already processed - return success with existing inbox id
            return ReceivePaymentWebhookResult.Duplicate(existingInbox.Id);
        }

        // Verify webhook signature through payment gateway adapter
        var verifyResult = await paymentGateway.VerifyWebhookAsync(
            new VerifyWebhookRequest
            {
                Provider = request.Provider,
                WebhookId = request.ProviderEventId,
                Payload = request.Payload,
                Signature = request.Signature
            },
            cancellationToken);

        if (!verifyResult.Verified)
        {
            // Create inbox record with signature verification failed
            var failedInbox = BillingWebhookInbox.Create(
                provider: request.Provider,
                providerEventId: request.ProviderEventId,
                eventType: request.EventType,
                signatureVerified: false,
                payloadHash: ComputeSha256Hash(request.Payload),
                providerOccurredAt: request.ProviderOccurredAt);

            await inboxRepository.AddAsync(failedInbox, cancellationToken);
            failedInbox.MarkFailed($"Signature verification failed: {verifyResult.ErrorMessage}");
            await inboxRepository.SaveChangesAsync(cancellationToken);

            return ReceivePaymentWebhookResult.Failure(verifyResult.ErrorCode, verifyResult.ErrorMessage);
        }

        // Signature verified - create inbox record
        var inbox = BillingWebhookInbox.Create(
            provider: request.Provider,
            providerEventId: request.ProviderEventId,
            eventType: request.EventType,
            signatureVerified: true,
            payloadHash: ComputeSha256Hash(request.Payload),
            providerOccurredAt: request.ProviderOccurredAt);

        await inboxRepository.AddAsync(inbox, cancellationToken);
        await inboxRepository.SaveChangesAsync(cancellationToken);

        // Process the webhook (business logic would go here)
        // For now, mark as processed
        inbox.MarkProcessed();
        await inboxRepository.SaveChangesAsync(cancellationToken);

        return ReceivePaymentWebhookResult.Accepted(inbox.Id);
    }

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Request to receive a payment webhook.
/// </summary>
public sealed class ReceivePaymentWebhookRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public DateTime? ProviderOccurredAt { get; set; }
}

/// <summary>
/// Result of receiving a payment webhook.
/// </summary>
public sealed class ReceivePaymentWebhookResult
{
    public bool Success { get; private set; }
    public Guid? InboxId { get; private set; }
    public bool IsDuplicate { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ReceivePaymentWebhookResult() { }

    public static ReceivePaymentWebhookResult Accepted(Guid inboxId) => new()
    {
        Success = true,
        InboxId = inboxId
    };

    public static ReceivePaymentWebhookResult Duplicate(Guid inboxId) => new()
    {
        Success = true,
        InboxId = inboxId,
        IsDuplicate = true
    };

    public static ReceivePaymentWebhookResult Failure(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Repository interface for BillingWebhookInbox.
/// </summary>
public interface IBillingWebhookInboxRepository
{
    Task<BillingWebhookInbox?> GetByProviderAndEventIdAsync(
        string provider,
        string providerEventId,
        CancellationToken cancellationToken = default);

    Task AddAsync(BillingWebhookInbox inbox, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
