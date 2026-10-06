using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Domain.E02;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Repositories.E02;

public sealed class EfOrganisationRepository(AppDbContext dbContext) : IOrganisationRepository
{
    public async Task AddAsync(Organisation organisation, CancellationToken cancellationToken = default)
    {
        await dbContext.Organisations.AddAsync(organisation, cancellationToken);
    }

    public async Task<Organisation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Organisations
            .Include(o => o.Identifiers)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Organisation>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.Organisations
            .Include(o => o.Identifiers)
            .Where(o => ids.Contains(o.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Organisation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Organisations
            .Include(o => o.Identifiers)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class EfOrganisationMembershipRepository(AppDbContext dbContext) : IOrganisationMembershipRepository
{
    public async Task AddAsync(OrganisationMembership membership, CancellationToken cancellationToken = default)
    {
        await dbContext.OrganisationMemberships.AddAsync(membership, cancellationToken);
    }

    public async Task<OrganisationMembership?> GetAsync(Guid organisationId, Guid userAccountId, CancellationToken cancellationToken = default)
    {
        return await dbContext.OrganisationMemberships
            .FirstOrDefaultAsync(m => m.OrganisationId == organisationId && m.UserAccountId == userAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganisationMembership>> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken = default)
    {
        return await dbContext.OrganisationMemberships
            .Where(m => m.UserAccountId == userAccountId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganisationMembership>> GetByOrganisationIdAsync(Guid organisationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.OrganisationMemberships
            .Where(m => m.OrganisationId == organisationId)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
