using System;
using System.Threading.Tasks;
using IndiaWorks.Application.Features.E04.Create;
using IndiaWorks.Application.Features.E04.Create.Dtos;
using Xunit;

namespace IndiaWorks.Application.Tests.E04.Create
{
    public class CreateTests
    {
        [Fact]
        public void IPaymentGateway_InterfaceIsDefined()
        {
            // Act
            var type = typeof(IPaymentGateway);

            // Assert
            Assert.NotNull(type);
            Assert.True(type.IsInterface);
            var createMethod = type.GetMethod(nameof(IPaymentGateway.CreateCheckoutSessionAsync));
            Assert.NotNull(createMethod);
            Assert.Equal(typeof(Task<CreateCheckoutSessionResult>), createMethod?.ReturnType);
            var cancelMethod = type.GetMethod(nameof(IPaymentGateway.CancelCheckoutSessionAsync));
            Assert.NotNull(cancelMethod);
            Assert.Equal(typeof(Task<CancelCheckoutSessionResult>), cancelMethod?.ReturnType);
            var verifyMethod = type.GetMethod(nameof(IPaymentGateway.VerifyWebhookAsync));
            Assert.NotNull(verifyMethod);
            Assert.Equal(typeof(Task<VerifyWebhookResult>), verifyMethod?.ReturnType);
        }

        [Fact]
        public void CreateCheckoutSessionRequest_HasExpectedProperties()
        {
            // Arrange
            var request = new CreateCheckoutSessionRequest
            {
                OrganisationId = Guid.NewGuid(),
                SubscriptionId = Guid.NewGuid(),
                SuccessUrl = "https://example.com/success",
                CancelUrl = "https://example.com/cancel",
                Metadata = "test-metadata"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, request.OrganisationId);
            Assert.NotEqual(Guid.Empty, request.SubscriptionId);
            Assert.Equal("https://example.com/success", request.SuccessUrl);
            Assert.Equal("https://example.com/cancel", request.CancelUrl);
            Assert.Equal("test-metadata", request.Metadata);
        }

        [Fact]
        public void CreateCheckoutSessionResult_HasExpectedProperties()
        {
            // Arrange
            var result = new CreateCheckoutSessionResult
            {
                CheckoutUrl = "https://checkout.example.com",
                SessionId = "sess_123",
                ProviderSessionId = "prov_sess_456",
                Success = true,
                ErrorCode = "NONE",
                ErrorMessage = string.Empty
            };

            // Assert
            Assert.Equal("https://checkout.example.com", result.CheckoutUrl);
            Assert.Equal("sess_123", result.SessionId);
            Assert.Equal("prov_sess_456", result.ProviderSessionId);
            Assert.True(result.Success);
            Assert.Equal("NONE", result.ErrorCode);
            Assert.Equal(string.Empty, result.ErrorMessage);
        }

        [Fact]
        public void CancelCheckoutSessionRequest_HasExpectedProperties()
        {
            // Arrange
            var request = new CancelCheckoutSessionRequest
            {
                SessionId = "sess_123",
                Reason = "User cancelled"
            };

            // Assert
            Assert.Equal("sess_123", request.SessionId);
            Assert.Equal("User cancelled", request.Reason);
        }

        [Fact]
        public void CancelCheckoutSessionResult_HasExpectedProperties()
        {
            // Arrange
            var result = new CancelCheckoutSessionResult
            {
                Success = true,
                ErrorCode = "NONE",
                ErrorMessage = string.Empty
            };

            // Assert
            Assert.True(result.Success);
            Assert.Equal("NONE", result.ErrorCode);
            Assert.Equal(string.Empty, result.ErrorMessage);
        }

        [Fact]
        public void VerifyWebhookRequest_HasExpectedProperties()
        {
            // Arrange
            var request = new VerifyWebhookRequest
            {
                Provider = "stripe",
                WebhookId = "wh_123",
                Payload = "{\"id\":\"evt_123\"}",
                Signature = "tss_123"
            };

            // Assert
            Assert.Equal("stripe", request.Provider);
            Assert.Equal("wh_123", request.WebhookId);
            Assert.Equal("{\"id\":\"evt_123\"}", request.Payload);
            Assert.Equal("tss_123", request.Signature);
        }

        [Fact]
        public void VerifyWebhookResult_HasExpectedProperties()
        {
            // Arrange
            var result = new VerifyWebhookResult
            {
                Verified = true,
                ErrorCode = "NONE",
                ErrorMessage = string.Empty,
                IsDuplicate = false
            };

            // Assert
            Assert.True(result.Verified);
            Assert.Equal("NONE", result.ErrorCode);
            Assert.Equal(string.Empty, result.ErrorMessage);
            Assert.False(result.IsDuplicate);
        }
    }
}