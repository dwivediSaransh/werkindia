using System;

namespace IndiaWorks.Application.Features.E04.Create.Dtos
{
    /// <summary>
    /// Request to create a checkout session.
    /// </summary>
    public class CreateCheckoutSessionRequest
    {
        public Guid OrganisationId { get; set; }
        public Guid SubscriptionId { get; set; }
        public string? SuccessUrl { get; set; }
        public string? CancelUrl { get; set; }
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// Result of creating a checkout session.
    /// </summary>
    public class CreateCheckoutSessionResult
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string ProviderSessionId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to cancel a checkout session.
    /// </summary>
    public class CancelCheckoutSessionRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Result of canceling a checkout session.
    /// </summary>
    public class CancelCheckoutSessionResult
    {
        public bool Success { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to verify a webhook from a payment provider.
    /// </summary>
    public class VerifyWebhookRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string WebhookId { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of verifying a webhook.
    /// </summary>
    public class VerifyWebhookResult
    {
        public bool Verified { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsDuplicate { get; set; }
    }
}