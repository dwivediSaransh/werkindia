using System.Threading;
using System.Threading.Tasks;
using IndiaWorks.Application.Features.E04.Create.Dtos;

namespace IndiaWorks.Application.Features.E04.Create
{
    /// <summary>
    /// Abstraction for a payment gateway that provides provider-neutral operations
    /// for creating checkout sessions, canceling sessions, and verifying webhooks.
    /// </summary>
    public interface IPaymentGateway
    {
        Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(
            CreateCheckoutSessionRequest request,
            CancellationToken cancellationToken = default);

        Task<CancelCheckoutSessionResult> CancelCheckoutSessionAsync(
            CancelCheckoutSessionRequest request,
            CancellationToken cancellationToken = default);

        Task<VerifyWebhookResult> VerifyWebhookAsync(
            VerifyWebhookRequest request,
            CancellationToken cancellationToken = default);
    }
}