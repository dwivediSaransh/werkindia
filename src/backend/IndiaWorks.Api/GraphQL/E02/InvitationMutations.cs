using HotChocolate;
using HotChocolate.Types;
using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Domain.E02;

namespace IndiaWorks.Api.GraphQL.E02;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class InvitationMutations
{
    /// <summary>
    /// Invite a member to an organisation. Only Owners and Admins may invite.
    /// The role is supplied as a string (e.g. "Recruiter") and parsed server-side.
    /// Returns a structured payload — no exception on validation/auth failures.
    /// </summary>
    public async Task<InviteMemberResult> InviteMember(
        InviteMemberInput input,
        [Service] OrganisationMemberInviteService inviteService,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrganisationRole>(input.Role, ignoreCase: true, out var role))
        {
            return InviteMemberResult.Failure("VALIDATION_FAILED",
                $"Invalid role '{input.Role}'. Supported: Owner, Admin, Recruiter, Viewer, BillingAdmin.");
        }

        return await inviteService.InviteAsync(new InviteMemberRequest(
            OrganisationId: input.OrganisationId,
            InvitedByUserAccountId: input.InvitedByUserAccountId,
            InviteeIdentifierType: input.InviteeIdentifierType,
            InviteeIdentifierHash: input.InviteeIdentifierHash,
            Role: role,
            TokenHash: input.TokenHash,
            ExpiresAt: input.ExpiresAt), cancellationToken);
    }

    /// <summary>
    /// Accept an invitation by token. Idempotent if the caller already has active membership.
    /// </summary>
    public async Task<AcceptInvitationResult> AcceptInvitation(
        AcceptInvitationInput input,
        [Service] OrganisationMemberInviteService inviteService,
        CancellationToken cancellationToken)
    {
        return await inviteService.AcceptAsync(new AcceptInvitationRequest(
            InvitationId: input.InvitationId,
            AcceptedByUserAccountId: input.AcceptedByUserAccountId), cancellationToken);
    }

    /// <summary>
    /// Revoke a pending invitation. Only Owners and Admins may revoke.
    /// </summary>
    public async Task<RevokeInvitationResult> RevokeInvitation(
        RevokeInvitationInput input,
        [Service] OrganisationMemberInviteService inviteService,
        CancellationToken cancellationToken)
    {
        return await inviteService.RevokeAsync(new RevokeInvitationRequest(
            InvitationId: input.InvitationId,
            RevokedByUserAccountId: input.RevokedByUserAccountId), cancellationToken);
    }
}
