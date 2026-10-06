using IndiaWorks.Domain.E04;

namespace IndiaWorks.Application.Features.E04.Implement;

public interface IEntitlementEvaluatorRepository
{
    Task<OrganisationSubscription?> GetActiveSubscriptionAsync(Guid organisationId, CancellationToken cancellationToken = default);
    Task<PlanEntitlement?> GetPlanEntitlementAsync(Guid planVersionId, string entitlementDefinitionCode, CancellationToken cancellationToken = default);
    Task<EntitlementDefinition?> GetEntitlementDefinitionAsync(string code, CancellationToken cancellationToken = default);
    Task<decimal> GetUsageAsync(Guid organisationId, string entitlementDefinitionCode, DateTime windowStart, DateTime windowEnd, CancellationToken cancellationToken = default);
}