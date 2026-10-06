using IndiaWorks.Domain.E04;

namespace IndiaWorks.Application.Features.E04.Provision;

/// <summary>
/// Request to provision a candidate unlock for an organisation.
/// </summary>
public sealed record ProvisionCandidateUnlockRequest(
    Guid OrganisationId,
    Guid CallerUserId,
    Guid CandidateProfileId,
    string? IdempotencyKey = null);

/// <summary>
/// Result of provisioning a candidate unlock.
/// </summary>
public sealed record ProvisionCandidateUnlockResult(
    bool Success,
    string AccessSource, // FREE_ALLOWANCE, PAID_ENTITLEMENT, ADD_ON
    int FreeLimit,
    int FreeUsed,
    int FreeRemaining,
    bool PaywallRequired,
    string? ErrorCode = null,
    string? ErrorMessage = null);