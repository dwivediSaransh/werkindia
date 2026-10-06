using System;

namespace IndiaWorks.Domain.E03;

/// <summary>
/// Versioned snapshot of the canonical job taxonomy.
/// Supports lifecycle: Draft -> Active -> Retired, with effective dating.
/// </summary>
public sealed class TaxonomyVersion
{
    public long Id { get; private set; }
    public string SemanticVersion { get; private set; } = default!;
    public DateTime EffectiveFrom { get; private set; }
    public TaxonomyVersionStatus Status { get; private set; }
    public string? ReleaseNotes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TaxonomyVersion() { }

    public static TaxonomyVersion Create(
        string semanticVersion,
        DateTime effectiveFrom,
        TaxonomyVersionStatus status = TaxonomyVersionStatus.Draft,
        string? releaseNotes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(semanticVersion);

        return new TaxonomyVersion
        {
            Id = default,
            SemanticVersion = semanticVersion.Trim(),
            EffectiveFrom = effectiveFrom,
            Status = status,
            ReleaseNotes = string.IsNullOrWhiteSpace(releaseNotes) ? null : releaseNotes.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum TaxonomyVersionStatus
{
    Draft,
    Active,
    Retired
}
