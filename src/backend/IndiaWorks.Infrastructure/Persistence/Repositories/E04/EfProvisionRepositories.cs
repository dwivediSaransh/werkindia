using IndiaWorks.Application.Features.E04.Provision;
using IndiaWorks.Domain.E04;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Repositories.E04;

public sealed class EfOrganisationCandidateUnlockRepository(AppDbContext dbContext) : IOrganisationCandidateUnlockRepository
{
    public async Task<OrganisationCandidateUnlock?> GetByOrganisationAndCandidateAsync(
        Guid organisationId,
        Guid candidateProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OrganisationCandidateUnlocks
            .FirstOrDefaultAsync(u => u.OrganisationId == organisationId && u.CandidateProfileId == candidateProfileId, cancellationToken);
    }

    public async Task AddAsync(OrganisationCandidateUnlock unlock, CancellationToken cancellationToken = default)
    {
        await dbContext.OrganisationCandidateUnlocks.AddAsync(unlock, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class EfOrganisationCommercialAccessStateRepository(AppDbContext dbContext) : IOrganisationCommercialAccessStateRepository
{
    public async Task<OrganisationCommercialAccessState?> GetByOrganisationIdAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OrganisationCommercialAccessStates
            .FirstOrDefaultAsync(s => s.OrganisationId == organisationId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class EfCandidatePaywallEventRepository(AppDbContext dbContext) : ICandidatePaywallEventRepository
{
    public async Task AddAsync(CandidatePaywallEvent paywallEvent, CancellationToken cancellationToken = default)
    {
        await dbContext.CandidatePaywallEvents.AddAsync(paywallEvent, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}