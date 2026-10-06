namespace IndiaWorks.Domain.E04;

/// <summary>
/// Immutable record of a single entitlement usage event for an organisation.
/// The IdempotencyKey guarantees exactly-once processing for retries.
/// </summary>
public sealed class EntitlementUsageEvent
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public string EntitlementDefinitionCode { get; private set; } = default!;
    public Guid PlanVersionId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime EventDate { get; private set; }
    public string IdempotencyKey { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private EntitlementUsageEvent() { }

    public static EntitlementUsageEvent Create(
        Guid organisationId,
        string entitlementDefinitionCode,
        Guid planVersionId,
        decimal quantity,
        string idempotencyKey)
    {
        if (organisationId == Guid.Empty)
            throw new ArgumentException("OrganisationId must not be empty.", nameof(organisationId));

        ArgumentException.ThrowIfNullOrWhiteSpace(entitlementDefinitionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return new EntitlementUsageEvent
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            EntitlementDefinitionCode = entitlementDefinitionCode.Trim(),
            PlanVersionId = planVersionId,
            Quantity = quantity,
            EventDate = DateTime.UtcNow,
            IdempotencyKey = idempotencyKey.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}