using IndiaWorks.Application.Features.E04.Implement;
using IndiaWorks.Domain.E04;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Repositories.E04;

public sealed class EfEntitlementEvaluatorRepository(AppDbContext dbContext) : IEntitlementEvaluatorRepository
{
    public async Task<OrganisationSubscription?> GetActiveSubscriptionAsync(Guid organisationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.OrganisationSubscriptions
            .Where(s => s.OrganisationId == organisationId)
            .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PlanEntitlement?> GetPlanEntitlementAsync(Guid planVersionId, string entitlementDefinitionCode, CancellationToken cancellationToken = default)
    {
        return await dbContext.PlanEntitlements
            .FirstOrDefaultAsync(
                pe => pe.PlanVersionId == planVersionId && pe.EntitlementDefinitionCode == entitlementDefinitionCode,
                cancellationToken);
    }

    public async Task<EntitlementDefinition?> GetEntitlementDefinitionAsync(string code, CancellationToken cancellationToken = default)
    {
        return await dbContext.EntitlementDefinitions
            .FirstOrDefaultAsync(ed => ed.Code == code, cancellationToken);
    }

    public async Task<decimal> GetUsageAsync(Guid organisationId, string entitlementDefinitionCode, DateTime windowStart, DateTime windowEnd, CancellationToken cancellationToken = default)
    {
        return await dbContext.EntitlementUsageEvents
            .Where(e => e.OrganisationId == organisationId)
            .Where(e => e.EntitlementDefinitionCode == entitlementDefinitionCode)
            .Where(e => e.EventDate >= windowStart && e.EventDate < windowEnd)
            .Select(e => e.Quantity)
            .SumAsync(cancellationToken);
    }
}