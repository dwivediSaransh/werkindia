using IndiaWorks.Domain.E02;

namespace IndiaWorks.Application.Features.E02.Implement;

// ── Invite ──────────────────────────────────────────────────────────────────

public sealed record InviteMemberRequest(
    Guid OrganisationId,
    Guid InvitedByUserAccountId,
    string InviteeIdentifierType,
    byte[] InviteeIdentifierHash,
    OrganisationRole Role,
    byte[] TokenHash,
    DateTime ExpiresAt);

public sealed record InviteMemberResult(
    bool IsSuccess,
    Guid? InvitationId = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static InviteMemberResult Success(Guid invitationId) =>
        new(true, invitationId);

    public static InviteMemberResult Failure(string errorCode, string errorMessage) =>
        new(false, null, errorCode, errorMessage);
}

// ── Accept ───────────────────────────────────────────────────────────────────

public sealed record AcceptInvitationRequest(
    Guid InvitationId,
    Guid AcceptedByUserAccountId);

public sealed record AcceptInvitationResult(
    bool IsSuccess,
    Guid? OrganisationId = null,
    OrganisationRole? Role = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static AcceptInvitationResult Success(Guid organisationId, OrganisationRole role) =>
        new(true, organisationId, role);

    public static AcceptInvitationResult Failure(string errorCode, string errorMessage) =>
        new(false, null, null, errorCode, errorMessage);
}

// ── Revoke ───────────────────────────────────────────────────────────────────

public sealed record RevokeInvitationRequest(
    Guid InvitationId,
    Guid RevokedByUserAccountId);

public sealed record RevokeInvitationResult(
    bool IsSuccess,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static RevokeInvitationResult Success() =>
        new(true);

    public static RevokeInvitationResult Failure(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage);
}
