namespace IndiaWorks.Domain.E04;

/// <summary>
/// Records a candidate paywall event when an organisation exhausts
/// its free candidate unlock quota and has no paid entitlement.
/// Persists the shown reason, job/context, free usage counters,
/// and conversion correlation identifiers.
/// </summary>
public sealed class CandidatePaywallEvent
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid CandidateProfileId { get; private set; }
    public string Reason { get; private set; } = default!;
    public string JobContext { get; private set; } = default!;
    public int FreeLimit { get; private set; }
    public int FreeUsed { get; private set; }
    public int FreeRemaining { get; private set; }
    public AccessSource AccessSource { get; private set; }
    public Guid CorrelationId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CandidatePaywallEvent() { }

    public static CandidatePaywallEvent Create(
        Guid organisationId,
        Guid candidateProfileId,
        string reason,
        string jobContext,
        int freeLimit,
        int freeUsed,
        int freeRemaining,
        AccessSource accessSource,
        Guid correlationId)
    {
        if (organisationId == Guid.Empty)
            throw new ArgumentException("OrganisationId must not be empty.", nameof(organisationId));

        if (candidateProfileId == Guid.Empty)
            throw new ArgumentException("CandidateProfileId must not be empty.", nameof(candidateProfileId));

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobContext);

        if (freeLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(freeLimit), "FreeLimit must not be negative.");

        if (freeUsed < 0)
            throw new ArgumentOutOfRangeException(nameof(freeUsed), "FreeUsed must not be negative.");

        if (freeRemaining < 0)
            throw new ArgumentOutOfRangeException(nameof(freeRemaining), "FreeRemaining must not be negative.");

        if (freeUsed > freeLimit)
            throw new ArgumentException("FreeUsed must not exceed FreeLimit.", nameof(freeUsed));

        if (freeRemaining != freeLimit - freeUsed)
            throw new ArgumentException("FreeRemaining must equal FreeLimit minus FreeUsed.", nameof(freeRemaining));

        if (correlationId == Guid.Empty)
            throw new ArgumentException("CorrelationId must not be empty.", nameof(correlationId));

        return new CandidatePaywallEvent
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            CandidateProfileId = candidateProfileId,
            Reason = reason.Trim(),
            JobContext = jobContext.Trim(),
            FreeLimit = freeLimit,
            FreeUsed = freeUsed,
            FreeRemaining = freeRemaining,
            AccessSource = accessSource,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
