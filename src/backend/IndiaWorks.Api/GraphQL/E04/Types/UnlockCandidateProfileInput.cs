using System;

namespace IndiaWorks.Api.GraphQL.E04.Types;

/// <summary>
/// Input for the unlock candidate profile mutation.
/// </summary>
public sealed record UnlockCandidateProfileInput
{
    public Guid OrganisationId { get; init; }
    public Guid CandidateProfileId { get; init; }
    public Guid CallerUserId { get; init; }
    public string? IdempotencyKey { get; init; }
}