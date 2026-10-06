using System;

namespace IndiaWorks.Api.GraphQL.E04.Types;

/// <summary>
/// Result of the unlock candidate profile mutation.
/// </summary>
public sealed record UnlockCandidateProfileResult
{
    public bool Success { get; init; }
    public string AccessSource { get; init; } = string.Empty;
    public int FreeLimit { get; init; }
    public int FreeUsed { get; init; }
    public int FreeRemaining { get; init; }
    public bool PaywallRequired { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}