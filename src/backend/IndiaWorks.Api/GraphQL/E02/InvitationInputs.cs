namespace IndiaWorks.Api.GraphQL.E02;

// ── Invite ───────────────────────────────────────────────────────────────────

public sealed record InviteMemberInput(
    Guid OrganisationId,
    Guid InvitedByUserAccountId,
    string InviteeIdentifierType,
    byte[] InviteeIdentifierHash,
    string Role,
    byte[] TokenHash,
    DateTime ExpiresAt);

// ── Accept ───────────────────────────────────────────────────────────────────

public sealed record AcceptInvitationInput(
    Guid InvitationId,
    Guid AcceptedByUserAccountId);

// ── Revoke ───────────────────────────────────────────────────────────────────

public sealed record RevokeInvitationInput(
    Guid InvitationId,
    Guid RevokedByUserAccountId);
