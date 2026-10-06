namespace IndiaWorks.Domain.E04;

/// <summary>
/// Tracks a single candidate profile unlock for an organisation.
/// Unique OrganisationId + CandidateProfileId combination; source indicates
/// FREE_ALLOWANCE, PAID_ENTITLEMENT, ADD_ON, or ADMIN.
/// </summary>
public sealed class OrganisationCandidateUnlock
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid CandidateProfileId { get; private set; }
    public UnlockSource Source { get; private set; }
    public DateTime UnlockedAt { get; private set; }

    private OrganisationCandidateUnlock() { }

    public static OrganisationCandidateUnlock Create(
        Guid organisationId,
        Guid candidateProfileId,
        UnlockSource source)
    {
        if (organisationId == Guid.Empty)
            throw new ArgumentException("OrganisationId must not be empty.", nameof(organisationId));

        if (candidateProfileId == Guid.Empty)
            throw new ArgumentException("CandidateProfileId must not be empty.", nameof(candidateProfileId));

        return new OrganisationCandidateUnlock
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            CandidateProfileId = candidateProfileId,
            Source = source,
            UnlockedAt = DateTime.UtcNow
        };
    }
}