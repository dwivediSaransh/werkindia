using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using IndiaWorks.Contracts.E04;
using IndiaWorks.Application.Features.E04.Receive;
using IndiaWorks.Application.Features.E04.Create.Dtos;
using IndiaWorks.Application.Features.E04.Create;
using IndiaWorks.Domain.E04;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ExposePaymentWebhookEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExposePaymentWebhookEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:IndiaWorksDb"] = "Server=localhost;Database=IndiaWorks_Test;Trusted_Connection=True;TrustServerCertificate=True;"
                });
            });
            builder.ConfigureServices(collection =>
            {
                collection.AddSingleton<FakeBillingWebhookInboxRepository>();
                collection.AddScoped<IBillingWebhookInboxRepository>(sp => sp.GetRequiredService<FakeBillingWebhookInboxRepository>());
                collection.AddSingleton<FakePaymentGateway>();
                collection.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<FakePaymentGateway>());
            });
        });
        _client = _factory.CreateClient();
    }

    private sealed class FakeBillingWebhookInboxRepository : IBillingWebhookInboxRepository
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, BillingWebhookInbox> _store = new();

        public Task<BillingWebhookInbox?> GetByProviderAndEventIdAsync(
            string provider,
            string providerEventId,
            CancellationToken cancellationToken = default)
        {
            var key = $"{provider}:{providerEventId}";
            _store.TryGetValue(key, out var inbox);
            return Task.FromResult<BillingWebhookInbox?>(inbox);
        }

        public Task AddAsync(BillingWebhookInbox inbox, CancellationToken cancellationToken = default)
        {
            var key = $"{inbox.Provider}:{inbox.ProviderEventId}";
            _store[key] = inbox;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Clear() => _store.Clear();
    }

    private sealed class FakePaymentGateway : IPaymentGateway
    {
        public bool ShouldVerify { get; set; } = true;
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(
            CreateCheckoutSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CancelCheckoutSessionResult> CancelCheckoutSessionAsync(
            CancelCheckoutSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<VerifyWebhookResult> VerifyWebhookAsync(
            VerifyWebhookRequest request,
            CancellationToken cancellationToken = default)
        {
            if (ShouldVerify)
            {
                return Task.FromResult(new VerifyWebhookResult
                {
                    Verified = true,
                    IsDuplicate = false
                });
            }
            return Task.FromResult(new VerifyWebhookResult
            {
                Verified = false,
                ErrorCode = ErrorCode,
                ErrorMessage = ErrorMessage
            });
        }
    }

    private static StringContent CreateJsonContent(object obj) =>
        new(JsonSerializer.Serialize(obj), System.Text.Encoding.UTF8, "application/json");

    [Fact]
    public async Task PostPaymentWebhook_happy_path_returns_inbox_id()
    {
        // Arrange
        var repo = _factory.Services.GetRequiredService<FakeBillingWebhookInboxRepository>();
        repo.Clear();

        var request = new PaymentWebhookRequest(
            Provider: "Stripe",
            ProviderEventId: "evt_test_123",
            EventType: "payment_intent.succeeded",
            Payload: """{"id":"pi_test","amount":1000,"currency":"inr"}""",
            Signature: "sig_test");

        var content = CreateJsonContent(request);

        // Act
        var response = await _client.PostAsync("/api/webhooks/payments/Stripe", content);

        // Assert
        Assert.Equal(200, (int)response.StatusCode);
        var responseBody = await response.Content.ReadFromJsonAsync<PaymentWebhookResponse>();
        Assert.NotNull(responseBody);
        Assert.True(responseBody.Success);
        Assert.NotNull(responseBody.InboxId);
        Assert.Null(responseBody.ErrorCode);
        Assert.Null(responseBody.ErrorMessage);
    }

    [Fact]
    public async Task PostPaymentWebhook_duplicate_event_returns_same_inbox_id()
    {
        // Arrange
        var repo = _factory.Services.GetRequiredService<FakeBillingWebhookInboxRepository>();
        repo.Clear();

        var request = new PaymentWebhookRequest(
            Provider: "Stripe",
            ProviderEventId: "evt_test_456",
            EventType: "payment_intent.succeeded",
            Payload: """{"id":"pi_test","amount":1000,"currency":"inr"}""",
            Signature: "sig_test");

        var content = CreateJsonContent(request);

        // Act - first request
        var response1 = await _client.PostAsync("/api/webhooks/payments/Stripe", content);
        Assert.Equal(200, (int)response1.StatusCode);
        var body1 = await response1.Content.ReadFromJsonAsync<PaymentWebhookResponse>();
        Assert.NotNull(body1);
        var inboxId1 = body1.InboxId;

        // Act - second request (duplicate)
        var response2 = await _client.PostAsync("/api/webhooks/payments/Stripe", content);

        // Assert - should succeed with same inbox ID
        Assert.Equal(200, (int)response2.StatusCode);
        var body2 = await response2.Content.ReadFromJsonAsync<PaymentWebhookResponse>();
        Assert.NotNull(body2);
        Assert.True(body2.Success);
        Assert.Equal(inboxId1, body2.InboxId);
    }

    [Fact]
    public async Task PostPaymentWebhook_invalid_signature_returns_error()
    {
        // Arrange
        var repo = _factory.Services.GetRequiredService<FakeBillingWebhookInboxRepository>();
        var gateway = _factory.Services.GetRequiredService<FakePaymentGateway>();
        repo.Clear();
        gateway.ShouldVerify = false;
        gateway.ErrorCode = "SIGNATURE_VERIFICATION_FAILED";
        gateway.ErrorMessage = "Invalid signature";

        var request = new PaymentWebhookRequest(
            Provider: "Stripe",
            ProviderEventId: "evt_test_789",
            EventType: "payment_intent.succeeded",
            Payload: """{"id":"pi_test","amount":1000,"currency":"inr"}""",
            Signature: "invalid_sig");

        var content = CreateJsonContent(request);

        // Act
        var response = await _client.PostAsync("/api/webhooks/payments/Stripe", content);

        // Assert
        Assert.Equal(400, (int)response.StatusCode);
        var responseBody = await response.Content.ReadFromJsonAsync<PaymentWebhookErrorResponse>();
        Assert.NotNull(responseBody);
        Assert.Equal("SIGNATURE_VERIFICATION_FAILED", responseBody.Code);
        Assert.Contains("Invalid signature", responseBody.Message);
    }

    [Fact]
    public async Task PostPaymentWebhook_missing_provider_event_id_uses_header()
    {
        // Arrange
        var repo = _factory.Services.GetRequiredService<FakeBillingWebhookInboxRepository>();
        repo.Clear();

        var request = new PaymentWebhookRequest(
            Provider: "Stripe",
            ProviderEventId: "", // empty, will use header
            EventType: "payment_intent.succeeded",
            Payload: """{"id":"pi_test","amount":1000,"currency":"inr"}""",
            Signature: "sig_test");

        var content = CreateJsonContent(request);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/payments/Stripe")
        {
            Content = content
        };
        requestMessage.Headers.Add("X-Provider-Event-Id", "evt_header_123");
        requestMessage.Headers.Add("X-Provider-Signature", "sig_header");

        // Act
        var response = await _client.SendAsync(requestMessage);

        // Assert
        Assert.Equal(200, (int)response.StatusCode);
        var responseBody = await response.Content.ReadFromJsonAsync<PaymentWebhookResponse>();
        Assert.NotNull(responseBody);
        Assert.True(responseBody.Success);
        Assert.NotNull(responseBody.InboxId);
    }
}
