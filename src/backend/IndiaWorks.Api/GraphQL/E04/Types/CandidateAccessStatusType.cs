using System;

namespace IndiaWorks.Api.GraphQL.E04.Types;

/// <summary>
/// Result of the candidate access status query.
/// </summary>
public sealed record CandidateAccessStatusResult
{
    public int FreeLimit { get; init; }
    public int FreeUsed { get; init; }
    public int FreeRemaining { get; init; }
    public bool PaywallActive { get; init; }
    public bool PaidAccessActive { get; init; }
}