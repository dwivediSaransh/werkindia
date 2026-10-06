using HotChocolate;
using HotChocolate.Types;
using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Api.GraphQL.E02.Types;

namespace IndiaWorks.Api.GraphQL.E02;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class InvitationQueries
{
    /// <summary>
    /// List pending invitations for an organisation.
    /// Caller must be an active Owner or Admin in the organisation (enforced by service).
    /// </summary>
    public async Task<IReadOnlyList<OrganisationInvitationSummary>> PendingInvitations(
        Guid organisationId,
        Guid callerUserAccountId,
        [Service] IOrganisationMembershipRepository membershipRepository,
        [Service] IOrganisationInvitationRepository invitationRepository,
        CancellationToken cancellationToken)
    {
        if (organisationId == Guid.Empty)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("OrganisationId is required.")
                .SetCode("VALIDATION_FAILED")
                .Build());
        }

        if (callerUserAccountId == Guid.Empty)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("CallerUserAccountId is required.")
                .SetCode("VALIDATION_FAILED")
                .Build());
        }

        // Authorization: caller must be active Owner or Admin
        var membership = await membershipRepository.GetAsync(organisationId, callerUserAccountId, cancellationToken);
        if (membership is null
            || membership.Status != Domain.E02.MembershipStatus.Active
            || (membership.Role != Domain.E02.OrganisationRole.Owner && membership.Role != Domain.E02.OrganisationRole.Admin))
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Only active Owners and Admins may list pending invitations.")
                .SetCode("ROLE_NOT_ALLOWED")
                .Build());
        }

        var invitations = await invitationRepository.GetByOrganisationIdAsync(organisationId, cancellationToken);
        return invitations
            .Where(i => i.Status == Domain.E02.InvitationStatus.Pending && i.ExpiresAt > DateTime.UtcNow)
            .Select(i => new OrganisationInvitationSummary
            {
                Id = i.Id,
                OrganisationId = i.OrganisationId,
                InvitedByUserAccountId = i.InvitedByUserAccountId,
                InviteeIdentifierType = i.InviteeIdentifierType,
                Role = i.Role.ToString(),
                Status = i.Status.ToString(),
                ExpiresAt = i.ExpiresAt,
                CreatedAt = i.CreatedAt
            })
            .ToList();
    }
}
