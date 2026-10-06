namespace IndiaWorks.Domain.E04;

/// <summary>
/// Tracks legal-entity-scoped free candidate unlock limit and provisioning state
/// for a single organisation. One organisation has exactly one access state row.
/// </summary>
public sealed class OrganisationCommercialAccessState
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public int FreeCandidateUnlockLimit { get; private set; }
    public int FreeCandidateUnlockUsed { get; private set; }
    public AccessProvisioningState ProvisioningState { get; private set; }
    public ResetCadence ResetCadence { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private OrganisationCommercialAccessState() { }

    public static OrganisationCommercialAccessState Create(
        Guid organisationId,
        int freeCandidateUnlockLimit,
        ResetCadence resetCadence)
    {
        if (organisationId == Guid.Empty)
            throw new ArgumentException("OrganisationId must not be empty.", nameof(organisationId));

        if (freeCandidateUnlockLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(freeCandidateUnlockLimit), "Limit must be positive.");

        return new OrganisationCommercialAccessState
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            FreeCandidateUnlockLimit = freeCandidateUnlockLimit,
            FreeCandidateUnlockUsed = 0,
            ProvisioningState = AccessProvisioningState.NotProvisioned,
            ResetCadence = resetCadence,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetProvisioned()
    {
        ProvisioningState = AccessProvisioningState.Provisioned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetExhausted()
    {
        ProvisioningState = AccessProvisioningState.Exhausted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementUsed()
    {
        FreeCandidateUnlockUsed++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetUsed()
    {
        FreeCandidateUnlockUsed = 0;
        UpdatedAt = DateTime.UtcNow;
    }
}