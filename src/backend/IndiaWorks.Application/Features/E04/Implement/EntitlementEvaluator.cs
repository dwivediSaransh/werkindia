using IndiaWorks.Domain.E04;
using Microsoft.Extensions.Logging;

namespace IndiaWorks.Application.Features.E04.Implement;

/// <summary>
/// Central entitlement evaluator. Determines whether an organisation
/// is ALLOWED, METERED, or DENIED for a given entitlement definition
/// based on its active subscription, plan version entitlement, and
/// current-period usage.
/// </summary>
public sealed class EntitlementEvaluator(
    IEntitlementEvaluatorRepository repository,
    ILogger<EntitlementEvaluator> logger)
{
    public async Task<EntitlementEvaluationResult> EvaluateAsync(
        EvaluateEntitlementRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate
        if (request.OrganisationId == Guid.Empty)
        {
            logger.LogWarning("Entitlement evaluation rejected: OrganisationId is empty.");
            return EntitlementEvaluationResult.Denied("VALIDATION_FAILED", "OrganisationId must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.EntitlementDefinitionCode))
        {
            logger.LogWarning(
                "Entitlement evaluation rejected for organisation {OrganisationId}: entitlement code is empty.",
                request.OrganisationId);
            return EntitlementEvaluationResult.Denied(
                "VALIDATION_FAILED",
                "EntitlementDefinitionCode must not be empty.");
        }

        var code = request.EntitlementDefinitionCode.Trim();

        logger.LogDebug(
            "Evaluating entitlement {EntitlementCode} for organisation {OrganisationId}",
            code,
            request.OrganisationId);

        // 2. Load active subscription
        var subscription = await repository.GetActiveSubscriptionAsync(request.OrganisationId, cancellationToken);
        if (subscription == null)
        {
            logger.LogWarning(
                "Entitlement evaluation denied for organisation {OrganisationId}: no active subscription",
                request.OrganisationId);
            return EntitlementEvaluationResult.Denied("FORBIDDEN", "No active subscription.");
        }

        if (subscription.Status != SubscriptionStatus.Active && subscription.Status != SubscriptionStatus.Trialing)
        {
            logger.LogWarning(
                "Entitlement evaluation denied for organisation {OrganisationId}: subscription status {Status}",
                request.OrganisationId,
                subscription.Status);
            return EntitlementEvaluationResult.Denied("FORBIDDEN", "Subscription is not active.");
        }

        // 3. Load plan entitlement
        var planEntitlement = await repository.GetPlanEntitlementAsync(
            subscription.PlanId,
            code,
            cancellationToken);

        if (planEntitlement == null || !planEntitlement.Enabled)
        {
            logger.LogWarning(
                "Entitlement evaluation denied for organisation {OrganisationId}: entitlement {EntitlementCode} not enabled on plan",
                request.OrganisationId,
                code);
            return EntitlementEvaluationResult.Denied("FORBIDDEN", "Entitlement not enabled on current plan.");
        }

        // 4. Load entitlement definition (for reset cadence)
        var entitlementDef = await repository.GetEntitlementDefinitionAsync(code, cancellationToken);
        if (entitlementDef == null)
        {
            logger.LogWarning(
                "Entitlement evaluation denied for organisation {OrganisationId}: entitlement definition {EntitlementCode} not found",
                request.OrganisationId,
                code);
            return EntitlementEvaluationResult.Denied("FORBIDDEN", "Entitlement definition not found.");
        }

        // 5. Calculate reset window
        var (windowStart, windowEnd) = CalculateWindow(entitlementDef.ResetCadence, subscription);

        // 6. Query current usage
        var used = await repository.GetUsageAsync(
            request.OrganisationId,
            code,
            windowStart,
            windowEnd,
            cancellationToken);

        var limit = planEntitlement.Limit ?? 0m;
        var remaining = limit - used;

        logger.LogInformation(
            "Entitlement evaluation result: organisation {OrganisationId}, entitlement {EntitlementCode}, decision {Decision}, used {Used}, limit {Limit}",
            request.OrganisationId,
            code,
            used < limit ? "ALLOW" : "METER",
            used,
            limit);

        return EntitlementEvaluationResult.Success(limit, used, windowStart, windowEnd);
    }

    private static (DateTime Start, DateTime End) CalculateWindow(ResetCadence cadence, OrganisationSubscription subscription)
    {
        var now = DateTime.UtcNow;
        return cadence switch
        {
            ResetCadence.None => (DateTime.MinValue, DateTime.MaxValue),
            ResetCadence.Daily => (now.Date, now.Date.AddDays(1)),
            ResetCadence.Monthly => (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1)),
            ResetCadence.Yearly => (new DateTime(now.Year, 1, 1), new DateTime(now.Year, 1, 1).AddYears(1)),
            ResetCadence.BillingPeriod => (subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd),
            _ => (DateTime.MinValue, DateTime.MaxValue),
        };
    }
}
