using IndiaWorks.Api.Routing;
using IndiaWorks.Application.Features.E04.Receive;
using IndiaWorks.Contracts.E04;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace IndiaWorks.Api.Rest.E04;

public static class ExposeEndpoints
{
    public static IEndpointRouteBuilder MapPaymentWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks/payments/{provider}");

        group.MapPost("/", ReceiveWebhook)
            .WithName(ApiEndpointNames.ReceivePaymentWebhook)
            .Produces<PaymentWebhookResponse>(StatusCodes.Status200OK)
            .Produces<PaymentWebhookErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<PaymentWebhookErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<PaymentWebhookErrorResponse>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ReceiveWebhook(
        [FromRoute] string provider,
        [FromBody] PaymentWebhookRequest request,
        [FromHeader(Name = "X-Provider-Event-Id")] string? providerEventIdHeader,
        [FromHeader(Name = "X-Provider-Signature")] string? signatureHeader,
        [FromHeader(Name = "X-Provider-Timestamp")] string? timestampHeader,
        ReceivePaymentWebhookHandler handler,
        CancellationToken cancellationToken)
    {
        // Build the handler request from route, body, and headers
        var handlerRequest = new ReceivePaymentWebhookRequest
        {
            Provider = provider.Trim(),
            ProviderEventId = providerEventIdHeader ?? request.ProviderEventId,
            EventType = request.EventType,
            Payload = request.Payload,
            Signature = signatureHeader ?? request.Signature,
            ProviderOccurredAt = DateTime.TryParse(timestampHeader, out var ts) ? ts : null
        };

        var result = await handler.HandleAsync(handlerRequest, cancellationToken);

        var response = new PaymentWebhookResponse(
            Success: result.Success,
            InboxId: result.InboxId?.ToString(),
            ErrorCode: result.ErrorCode,
            ErrorMessage: result.ErrorMessage);

        if (result.IsDuplicate)
        {
            return TypedResults.Ok(response);
        }

        if (!result.Success)
        {
            var statusCode = result.ErrorCode switch
            {
                "VALIDATION_FAILED" => StatusCodes.Status400BadRequest,
                "UNAUTHENTICATED" => StatusCodes.Status401Unauthorized,
                "FORBIDDEN" => StatusCodes.Status403Forbidden,
                "CONFLICT" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return Results.Json(new PaymentWebhookErrorResponse(result.ErrorCode!, result.ErrorMessage!), statusCode: statusCode);
        }

        return TypedResults.Ok(response);
    }
}