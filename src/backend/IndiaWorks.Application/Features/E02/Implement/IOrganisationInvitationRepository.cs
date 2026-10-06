using IndiaWorks.Domain.E02;

namespace IndiaWorks.Application.Features.E02.Implement;

public interface IOrganisationInvitationRepository
{
    Task AddAsync(OrganisationInvitation invitation, CancellationToken cancellationToken = default);
    Task<OrganisationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganisationInvitation>> GetByOrganisationIdAsync(
        Guid organisationId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the most recent pending invitation for the given org+invitee hash combination.
    /// </summary>
    Task<OrganisationInvitation?> GetPendingByOrgAndInviteeHashAsync(
        Guid organisationId, byte[] inviteeIdentifierHash, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
