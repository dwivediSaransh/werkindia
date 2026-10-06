using IndiaWorks.Domain.E02;

namespace IndiaWorks.Application.Features.E02.Implement;

public interface IOrganisationRepository
{
    Task AddAsync(Organisation organisation, CancellationToken cancellationToken = default);
    Task<Organisation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Organisation>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Organisation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IOrganisationMembershipRepository
{
    Task AddAsync(OrganisationMembership membership, CancellationToken cancellationToken = default);
    Task<OrganisationMembership?> GetAsync(Guid organisationId, Guid userAccountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganisationMembership>> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganisationMembership>> GetByOrganisationIdAsync(Guid organisationId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
