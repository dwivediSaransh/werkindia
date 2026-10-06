using System;

namespace IndiaWorks.Domain.E03;

/// <summary>
/// A family of related roles (e.g. "Delivery", "Healthcare", "Skilled Trades").
/// Supports hierarchy via ParentId and is versioned through TaxonomyVersion.
/// </summary>
public sealed class JobFamily
{
    public long Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public long? ParentId { get; private set; }
    public JobFamily? Parent { get; private set; }
    public long TaxonomyVersionId { get; private set; }
    public TaxonomyVersion? TaxonomyVersion { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private JobFamily() { }

    public static JobFamily Create(
        string code,
        string name,
        long taxonomyVersionId,
        long? parentId = null,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new JobFamily
        {
            Id = default,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            ParentId = parentId,
            TaxonomyVersionId = taxonomyVersionId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}
