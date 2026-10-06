namespace IndiaWorks.Domain.E04;

/// <summary>
/// Junction entity linking a plan version to an entitlement definition
/// with the per-version override (enabled/limit).
/// Composite key: (PlanVersionId, EntitlementDefinitionCode).
/// </summary>
public sealed class PlanEntitlement
{
    public Guid PlanVersionId { get; private set; }
    public string EntitlementDefinitionCode { get; private set; } = default!;
    public bool Enabled { get; private set; }
    public decimal? Limit { get; private set; }

    // Foreign-key-only: navigation properties omitted to avoid duplicate FK columns.

    private PlanEntitlement() { }

    public static PlanEntitlement Create(SubscriptionPlanVersion planVersion, EntitlementDefinition entitlementDefinition, bool enabled = true, decimal? limit = null)
    {
        if (planVersion == null)
            throw new ArgumentNullException(nameof(planVersion));

        if (entitlementDefinition == null)
            throw new ArgumentNullException(nameof(entitlementDefinition));

        return new PlanEntitlement
        {
            PlanVersionId = planVersion.Id,
            EntitlementDefinitionCode = entitlementDefinition.Code,
            Enabled = enabled,
            Limit = limit
        };
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
    }

    public void SetLimit(decimal? limit)
    {
        Limit = limit;
    }
}