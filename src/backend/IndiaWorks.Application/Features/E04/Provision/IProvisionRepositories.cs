using IndiaWorks.Domain.E04;

namespace IndiaWorks.Application.Features.E04.Provision;

/// <summary>
/// Repository for accessing OrganisationCandidateUnlock entities.
/// </summary>
public interface IOrganisationCandidateUnlockRepository
{
    Task<OrganisationCandidateUnlock?> GetByOrganisationAndCandidateAsync(
        Guid organisationId,
        Guid candidateProfileId,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganisationCandidateUnlock unlock, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for accessing OrganisationCommercialAccessState entities.
/// </summary>
public interface IOrganisationCommercialAccessStateRepository
{
    Task<OrganisationCommercialAccessState?> GetByOrganisationIdAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for accessing CandidatePaywallEvent entities.
/// </summary>
public interface ICandidatePaywallEventRepository
{
    Task AddAsync(CandidatePaywallEvent paywallEvent, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}