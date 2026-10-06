using IndiaWorks.Application.Features.E04.Create;
using IndiaWorks.Application.Features.E04.Create.Dtos;
using IndiaWorks.Application.Features.E04.Receive;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Application.Tests.E04;

public sealed class ReceivePaymentWebhookHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSignatureValid_PersistsInboxAsProcessed()
    {
        var repository = new InMemoryBillingWebhookInboxRepository();
        var gateway = new StubPaymentGateway(new VerifyWebhookResult { Verified = true });
        var handler = new ReceivePaymentWebhookHandler(repository, gateway);

        var result = await handler.HandleAsync(ValidRequest());

        Assert.True(result.Success);
        Assert.NotNull(result.InboxId);
        var inbox = Assert.Single(repository.Items);
        Assert.True(inbox.SignatureVerified);
        Assert.Equal("Processed", inbox.ProcessStatus);
        Assert.Equal(Sha256("payload-1"), inbox.PayloadHash);
        Assert.Equal(2, repository.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_WhenEventAlreadyExists_ReturnsDuplicateWithoutCallingProvider()
    {
        var repository = new InMemoryBillingWebhookInboxRepository();
        var existing = BillingWebhookInbox.Create(
            "stripe",
            "evt_123",
            "invoice.paid",
            signatureVerified: true,
            payloadHash: Sha256("payload-1"),
            providerOccurredAt: null);
        repository.Items.Add(existing);
        var gateway = new StubPaymentGateway(new VerifyWebhookResult { Verified = true });
        var handler = new ReceivePaymentWebhookHandler(repository, gateway);

        var result = await handler.HandleAsync(ValidRequest());

        Assert.True(result.Success);
        Assert.True(result.IsDuplicate);
        Assert.Equal(existing.Id, result.InboxId);
        Assert.Equal(0, gateway.VerifyCalls);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task HandleAsync_WhenSignatureInvalid_StoresFailedInboxWithStableError()
    {
        var repository = new InMemoryBillingWebhookInboxRepository();
        var gateway = new StubPaymentGateway(new VerifyWebhookResult
        {
            Verified = false,
            ErrorCode = "PAYMENT_WEBHOOK_SIGNATURE_INVALID",
            ErrorMessage = "Webhook signature is invalid."
        });
        var handler = new ReceivePaymentWebhookHandler(repository, gateway);

        var result = await handler.HandleAsync(ValidRequest());

        Assert.False(result.Success);
        Assert.Equal("PAYMENT_WEBHOOK_SIGNATURE_INVALID", result.ErrorCode);
        var inbox = Assert.Single(repository.Items);
        Assert.False(inbox.SignatureVerified);
        Assert.Equal("Failed", inbox.ProcessStatus);
        Assert.Equal(Sha256("payload-1"), inbox.PayloadHash);
    }

    private static ReceivePaymentWebhookRequest ValidRequest()
    {
        return new ReceivePaymentWebhookRequest
        {
            Provider = "stripe",
            ProviderEventId = "evt_123",
            EventType = "invoice.paid",
            Payload = "payload-1",
            Signature = "sha256=test"
        };
    }

    private static string Sha256(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

    private sealed class InMemoryBillingWebhookInboxRepository : IBillingWebhookInboxRepository
    {
        public List<BillingWebhookInbox> Items { get; } = [];
        public int SaveChangesCount { get; private set; }

        public Task<BillingWebhookInbox?> GetByProviderAndEventIdAsync(
            string provider,
            string providerEventId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(item =>
                item.Provider == provider && item.ProviderEventId == providerEventId));
        }

        public Task AddAsync(BillingWebhookInbox inbox, CancellationToken cancellationToken = default)
        {
            Items.Add(inbox);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubPaymentGateway(VerifyWebhookResult result) : IPaymentGateway
    {
        public int VerifyCalls { get; private set; }

        public Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(
            CreateCheckoutSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CancelCheckoutSessionResult> CancelCheckoutSessionAsync(
            CancelCheckoutSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<VerifyWebhookResult> VerifyWebhookAsync(
            VerifyWebhookRequest request,
            CancellationToken cancellationToken = default)
        {
            VerifyCalls++;
            return Task.FromResult(result);
        }
    }
}
