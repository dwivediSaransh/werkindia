using System.Security.Cryptography;
using System.Text;
using IndiaWorks.Application.Features.E04.Create;
using IndiaWorks.Application.Features.E04.Create.Dtos;
using Microsoft.Extensions.Configuration;

namespace IndiaWorks.Infrastructure.Payments.E04;

public sealed class ConfiguredPaymentGateway(IConfiguration configuration) : IPaymentGateway
{
    public Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CreateCheckoutSessionResult
        {
            Success = false,
            ErrorCode = "PAYMENT_PROVIDER_NOT_CONFIGURED",
            ErrorMessage = "Checkout provider is not configured."
        });
    }

    public Task<CancelCheckoutSessionResult> CancelCheckoutSessionAsync(
        CancelCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CancelCheckoutSessionResult
        {
            Success = false,
            ErrorCode = "PAYMENT_PROVIDER_NOT_CONFIGURED",
            ErrorMessage = "Checkout provider is not configured."
        });
    }

    public Task<VerifyWebhookResult> VerifyWebhookAsync(
        VerifyWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return Task.FromResult(Failed("VALIDATION_FAILED", "Provider is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Signature))
        {
            return Task.FromResult(Failed("PAYMENT_WEBHOOK_SIGNATURE_MISSING", "Webhook signature is required."));
        }

        var secret = configuration[$"PaymentWebhooks:{request.Provider.Trim()}:SigningSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Task.FromResult(Failed("PAYMENT_WEBHOOK_SECRET_MISSING", "Webhook signing secret is not configured."));
        }

        var expected = ComputeSignature(request.Payload ?? string.Empty, secret);
        var provided = NormalizeSignature(request.Signature);
        var verified = FixedTimeEquals(expected, provided);

        return Task.FromResult(verified
            ? new VerifyWebhookResult { Verified = true }
            : Failed("PAYMENT_WEBHOOK_SIGNATURE_INVALID", "Webhook signature is invalid."));
    }

    private static VerifyWebhookResult Failed(string code, string message)
    {
        return new VerifyWebhookResult
        {
            Verified = false,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string NormalizeSignature(string signature)
    {
        var trimmed = signature.Trim();
        return trimmed.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? trimmed["sha256=".Length..].Trim()
            : trimmed;
    }

    private static bool FixedTimeEquals(string expectedHex, string providedHex)
    {
        var expected = Encoding.UTF8.GetBytes(expectedHex);
        var provided = Encoding.UTF8.GetBytes(providedHex.ToLowerInvariant());
        return expected.Length == provided.Length && CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
