using IndiaWorks.Domain.E02;
using Microsoft.Extensions.Logging;

namespace IndiaWorks.Application.Features.E02.Implement;

/// <summary>
/// Application service for the invitation lifecycle: invite, accept, revoke.
/// Authorization rules:
///   - InvitedByUserAccountId must be an ACTIVE Owner or Admin in the target organisation.
///   - RevokedByUserAccountId must be an ACTIVE Owner or Admin in the target organisation.
///   - Accept: the invitation must be Pending and not expired; idempotent — if caller already
///     has an Active membership in the org, accept is silently re-accepted (same result returned).
/// </summary>
public sealed class OrganisationMemberInviteService(
    IOrganisationRepository organisationRepository,
    IOrganisationMembershipRepository membershipRepository,
    IOrganisationInvitationRepository invitationRepository,
    ILogger<OrganisationMemberInviteService> logger)
{
    private static readonly HashSet<OrganisationRole> AllowedInviterRoles =
        new() { OrganisationRole.Owner, OrganisationRole.Admin };

    // ── Invite ───────────────────────────────────────────────────────────────

    public async Task<InviteMemberResult> InviteAsync(
        InviteMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrganisationId == Guid.Empty)
            return InviteMemberResult.Failure("VALIDATION_FAILED", "OrganisationId must not be empty.");
        if (request.InvitedByUserAccountId == Guid.Empty)
            return InviteMemberResult.Failure("VALIDATION_FAILED", "InvitedByUserAccountId must not be empty.");
        if (string.IsNullOrWhiteSpace(request.InviteeIdentifierType))
            return InviteMemberResult.Failure("VALIDATION_FAILED", "InviteeIdentifierType must not be empty.");
        if (request.InviteeIdentifierHash is null || request.InviteeIdentifierHash.Length == 0)
            return InviteMemberResult.Failure("VALIDATION_FAILED", "InviteeIdentifierHash must not be empty.");
        if (request.TokenHash is null || request.TokenHash.Length == 0)
            return InviteMemberResult.Failure("VALIDATION_FAILED", "TokenHash must not be empty.");
        if (request.ExpiresAt <= DateTime.UtcNow)
            return InviteMemberResult.Failure("VALIDATION_FAILED", "ExpiresAt must be in the future.");

        // Organisation must exist
        var organisation = await organisationRepository.GetByIdAsync(request.OrganisationId, cancellationToken);
        if (organisation is null)
        {
            logger.LogWarning("Invite rejected: organisation {OrgId} not found.", request.OrganisationId);
            return InviteMemberResult.Failure("ORGANISATION_NOT_FOUND", "Organisation not found.");
        }

        // Inviter must be active Owner or Admin
        var inviterMembership = await membershipRepository.GetAsync(
            request.OrganisationId, request.InvitedByUserAccountId, cancellationToken);

        if (inviterMembership is null || inviterMembership.Status != MembershipStatus.Active)
        {
            logger.LogWarning("Invite rejected: caller {UserId} is not an active member of org {OrgId}.",
                request.InvitedByUserAccountId, request.OrganisationId);
            return InviteMemberResult.Failure("MEMBERSHIP_REQUIRED", "Caller must be an active member of the organisation.");
        }

        if (!AllowedInviterRoles.Contains(inviterMembership.Role))
        {
            logger.LogWarning("Invite rejected: caller {UserId} has role {Role}, not Owner/Admin.",
                request.InvitedByUserAccountId, inviterMembership.Role);
            return InviteMemberResult.Failure("ROLE_NOT_ALLOWED", "Only Owners and Admins may invite members.");
        }

        // Idempotency: revoke any existing pending invitation for same org+invitee before creating new one.
        var existingPending = await invitationRepository.GetPendingByOrgAndInviteeHashAsync(
            request.OrganisationId, request.InviteeIdentifierHash, cancellationToken);
        if (existingPending is not null)
        {
            existingPending.Revoke();
        }

        var invitation = OrganisationInvitation.Create(
            organisationId: request.OrganisationId,
            invitedByUserAccountId: request.InvitedByUserAccountId,
            inviteeIdentifierType: request.InviteeIdentifierType,
            inviteeIdentifierHash: request.InviteeIdentifierHash,
            role: request.Role,
            tokenHash: request.TokenHash,
            expiresAt: request.ExpiresAt);

        await invitationRepository.AddAsync(invitation, cancellationToken);
        await invitationRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Invitation {InvitationId} created for org {OrgId} by {UserId}.",
            invitation.Id, request.OrganisationId, request.InvitedByUserAccountId);

        return InviteMemberResult.Success(invitation.Id);
    }

    // ── Accept ───────────────────────────────────────────────────────────────

    public async Task<AcceptInvitationResult> AcceptAsync(
        AcceptInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.InvitationId == Guid.Empty)
            return AcceptInvitationResult.Failure("VALIDATION_FAILED", "InvitationId must not be empty.");
        if (request.AcceptedByUserAccountId == Guid.Empty)
            return AcceptInvitationResult.Failure("VALIDATION_FAILED", "AcceptedByUserAccountId must not be empty.");

        var invitation = await invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null)
            return AcceptInvitationResult.Failure("INVITATION_NOT_FOUND", "Invitation not found.");

        // Idempotency: if the caller already has active membership for this org+role, succeed silently.
        var existingMembership = await membershipRepository.GetAsync(
            invitation.OrganisationId, request.AcceptedByUserAccountId, cancellationToken);
        if (existingMembership is not null && existingMembership.Status == MembershipStatus.Active)
        {
            logger.LogInformation("Accept idempotent: user {UserId} already active in org {OrgId}.",
                request.AcceptedByUserAccountId, invitation.OrganisationId);
            return AcceptInvitationResult.Success(invitation.OrganisationId, existingMembership.Role);
        }

        // Validate lifecycle state
        if (invitation.Status != InvitationStatus.Pending)
            return AcceptInvitationResult.Failure("INVITATION_NOT_ACTIVE", $"Invitation is in state {invitation.Status}.");
        if (DateTime.UtcNow > invitation.ExpiresAt)
            return AcceptInvitationResult.Failure("INVITATION_EXPIRED", "Invitation has expired.");

        invitation.Accept(request.AcceptedByUserAccountId);

        var membership = OrganisationMembership.Create(
            organisationId: invitation.OrganisationId,
            userAccountId: request.AcceptedByUserAccountId,
            role: invitation.Role);

        await membershipRepository.AddAsync(membership, cancellationToken);
        await invitationRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Invitation {InvitationId} accepted. Membership created for user {UserId} in org {OrgId} with role {Role}.",
            request.InvitationId, request.AcceptedByUserAccountId, invitation.OrganisationId, invitation.Role);

        return AcceptInvitationResult.Success(invitation.OrganisationId, invitation.Role);
    }

    // ── Revoke ───────────────────────────────────────────────────────────────

    public async Task<RevokeInvitationResult> RevokeAsync(
        RevokeInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.InvitationId == Guid.Empty)
            return RevokeInvitationResult.Failure("VALIDATION_FAILED", "InvitationId must not be empty.");
        if (request.RevokedByUserAccountId == Guid.Empty)
            return RevokeInvitationResult.Failure("VALIDATION_FAILED", "RevokedByUserAccountId must not be empty.");

        var invitation = await invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null)
            return RevokeInvitationResult.Failure("INVITATION_NOT_FOUND", "Invitation not found.");

        // Revoker must be active Owner or Admin in the org
        var revokerMembership = await membershipRepository.GetAsync(
            invitation.OrganisationId, request.RevokedByUserAccountId, cancellationToken);

        if (revokerMembership is null || revokerMembership.Status != MembershipStatus.Active)
            return RevokeInvitationResult.Failure("MEMBERSHIP_REQUIRED", "Caller must be an active member of the organisation.");

        if (!AllowedInviterRoles.Contains(revokerMembership.Role))
            return RevokeInvitationResult.Failure("ROLE_NOT_ALLOWED", "Only Owners and Admins may revoke invitations.");

        invitation.Revoke();
        await invitationRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Invitation {InvitationId} revoked by {UserId}.",
            request.InvitationId, request.RevokedByUserAccountId);

        return RevokeInvitationResult.Success();
    }
}
