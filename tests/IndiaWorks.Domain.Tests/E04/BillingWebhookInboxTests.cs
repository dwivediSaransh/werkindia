using IndiaWorks.Domain.E04;
using Xunit;

namespace IndiaWorks.Domain.Tests.E04;

public sealed class BillingWebhookInboxTests
{
    [Fact]
    public void Create_WithValidParameters_CapturesWebhookEvent()
    {
        var provider = "Stripe";
        var providerEventId = "evt_1234567890";
        var eventType = "invoice.payment_succeeded";
        var signatureVerified = true;
        var payloadHash = "sha256:5d41402abc4b2a76b9719d911017c592";
        var providerOccurredAt = DateTime.UtcNow.AddMinutes(-5);

        var webhookInbox = BillingWebhookInbox.Create(
            provider,
            providerEventId,
            eventType,
            signatureVerified,
            payloadHash,
            providerOccurredAt);

        Assert.NotEqual(Guid.Empty, webhookInbox.Id);
        Assert.Equal(provider, webhookInbox.Provider);
        Assert.Equal(providerEventId, webhookInbox.ProviderEventId);
        Assert.Equal(eventType, webhookInbox.EventType);
        Assert.Equal(signatureVerified, webhookInbox.SignatureVerified);
        Assert.Equal(payloadHash, webhookInbox.PayloadHash);
        Assert.Equal(providerOccurredAt, webhookInbox.ProviderOccurredAt);
        Assert.True(webhookInbox.ReceivedAt <= DateTime.UtcNow);
        Assert.Equal("Received", webhookInbox.ProcessStatus);
        Assert.Null(webhookInbox.ProcessedAt);
        Assert.Null(webhookInbox.ErrorMessage);
    }

    [Fact]
    public void Create_WithNullProviderOccurredAt_Works()
    {
        var webhookInbox = BillingWebhookInbox.Create(
            "Stripe",
            "evt_1234567890",
            "invoice.payment_succeeded",
            true,
            "sha256:5d41402abc4b2a76b9719d911017c592",
            null);

        Assert.Null(webhookInbox.ProviderOccurredAt);
    }

    [Fact]
    public void Create_WithEmptyProvider_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(provider: " "));
    }

    [Fact]
    public void Create_WithEmptyProviderEventId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(providerEventId: " "));
    }

    [Fact]
    public void Create_WithEmptyEventType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(eventType: " "));
    }

    [Fact]
    public void Create_WithEmptyPayloadHash_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateValid(payloadHash: " "));
    }

    [Fact]
    public void MarkProcessed_UpdatesStatus()
    {
        var webhookInbox = CreateValid();

        webhookInbox.MarkProcessed();

        Assert.Equal("Processed", webhookInbox.ProcessStatus);
        Assert.NotNull(webhookInbox.ProcessedAt);
        Assert.Null(webhookInbox.ErrorMessage);
    }

    [Fact]
    public void MarkFailed_UpdatesStatusWithError()
    {
        var webhookInbox = CreateValid();

        webhookInbox.MarkFailed("Signature verification failed");

        Assert.Equal("Failed", webhookInbox.ProcessStatus);
        Assert.NotNull(webhookInbox.ProcessedAt);
        Assert.Equal("Signature verification failed", webhookInbox.ErrorMessage);
    }

    [Fact]
    public void MarkFailed_WithEmptyErrorMessage_HandlesGracefully()
    {
        var webhookInbox = CreateValid();

        webhookInbox.MarkFailed(string.Empty);

        Assert.Equal("Failed", webhookInbox.ProcessStatus);
        Assert.NotNull(webhookInbox.ProcessedAt);
        Assert.Equal(string.Empty, webhookInbox.ErrorMessage);
    }

    [Fact]
    public void MarkFailed_WithNullErrorMessage_HandlesGracefully()
    {
        var webhookInbox = CreateValid();

        webhookInbox.MarkFailed(null);

        Assert.Equal("Failed", webhookInbox.ProcessStatus);
        Assert.NotNull(webhookInbox.ProcessedAt);
        Assert.Null(webhookInbox.ErrorMessage);
    }

    private static BillingWebhookInbox CreateValid(
        string provider = "Stripe",
        string providerEventId = "evt_1234567890",
        string eventType = "invoice.payment_succeeded",
        bool signatureVerified = true,
        string payloadHash = "sha256:5d41402abc4b2a76b9719d911017c592",
        DateTime? providerOccurredAt = null)
    {
        return BillingWebhookInbox.Create(
            provider,
            providerEventId,
            eventType,
            signatureVerified,
            payloadHash,
            providerOccurredAt ?? DateTime.UtcNow.AddMinutes(-5));
    }
}
