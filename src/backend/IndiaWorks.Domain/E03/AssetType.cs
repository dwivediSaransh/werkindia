using System;

namespace IndiaWorks.Domain.E03;

/// <summary>
/// An asset type that can be required or possessed (e.g. driver license, vehicle, tools).
/// Versioned through TaxonomyVersion.
/// </summary>
public sealed class AssetType
{
    public long Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public long TaxonomyVersionId { get; private set; }
    public TaxonomyVersion? TaxonomyVersion { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AssetType() { }

    public static AssetType Create(
        string code,
        string name,
        long taxonomyVersionId,
        string? description = null,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new AssetType
        {
            Id = default,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            TaxonomyVersionId = taxonomyVersionId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}
