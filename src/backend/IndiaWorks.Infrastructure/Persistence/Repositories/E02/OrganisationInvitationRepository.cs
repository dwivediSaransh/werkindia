using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Domain.E02;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Repositories.E02;

public sealed class EfOrganisationInvitationRepository(AppDbContext dbContext)
    : IOrganisationInvitationRepository
{
    public async Task AddAsync(OrganisationInvitation invitation, CancellationToken cancellationToken = default)
    {
        await dbContext.OrganisationInvitations.AddAsync(invitation, cancellationToken);
    }

    public async Task<OrganisationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.OrganisationInvitations
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganisationInvitation>> GetByOrganisationIdAsync(
        Guid organisationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.OrganisationInvitations
            .Where(i => i.OrganisationId == organisationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrganisationInvitation?> GetPendingByOrgAndInviteeHashAsync(
        Guid organisationId, byte[] inviteeIdentifierHash, CancellationToken cancellationToken = default)
    {
        return await dbContext.OrganisationInvitations
            .Where(i => i.OrganisationId == organisationId
                     && i.Status == InvitationStatus.Pending
                     && i.InviteeIdentifierHash == inviteeIdentifierHash)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
