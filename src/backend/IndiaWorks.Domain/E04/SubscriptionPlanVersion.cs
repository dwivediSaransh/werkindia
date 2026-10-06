namespace IndiaWorks.Domain.E04;

/// <summary>
/// Immutable version of a subscription plan. Once created the pricing
/// and trial fields cannot be changed; a new version must be added
/// to the parent plan for any modification.
/// </summary>
public sealed class SubscriptionPlanVersion
{
    public Guid Id { get; private set; }
    public Guid PlanId { get; private set; }
    public BillingInterval Interval { get; private set; }
    public string Currency { get; private set; } = default!;
    public decimal? Amount { get; private set; }
    public int? TrialDays { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SubscriptionPlanVersion() { }

    public static SubscriptionPlanVersion Create(Guid planId, BillingInterval interval, string currency, decimal? amount = null, int? trialDays = null)
    {
        if (planId == Guid.Empty)
            throw new ArgumentException("PlanId must not be empty.", nameof(planId));

        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return new SubscriptionPlanVersion
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            Interval = interval,
            Currency = currency.Trim().ToUpperInvariant(),
            Amount = amount,
            TrialDays = trialDays,
            CreatedAt = DateTime.UtcNow
        };
    }
}