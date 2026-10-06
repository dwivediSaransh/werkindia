namespace IndiaWorks.Domain.E04;

/// <summary>
/// Links an organisation to a subscription plan and tracks the current billing period.
/// One organisation has at most one active subscription at any time.
/// </summary>
public sealed class OrganisationSubscription
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime CurrentPeriodStart { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private OrganisationSubscription() { }

    public static OrganisationSubscription Create(
        Guid organisationId,
        Guid planId,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd)
    {
        if (organisationId == Guid.Empty)
            throw new ArgumentException("OrganisationId must not be empty.", nameof(organisationId));

        if (planId == Guid.Empty)
            throw new ArgumentException("PlanId must not be empty.", nameof(planId));

        return new OrganisationSubscription
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            PlanId = planId,
            Status = SubscriptionStatus.Trialing,
            CurrentPeriodStart = currentPeriodStart,
            CurrentPeriodEnd = currentPeriodEnd,
            CancelAtPeriodEnd = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateStatus(SubscriptionStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCancelAtPeriodEnd(bool cancel)
    {
        CancelAtPeriodEnd = cancel;
        UpdatedAt = DateTime.UtcNow;
    }
}