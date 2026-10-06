using System;

namespace IndiaWorks.Domain.E03;

/// <summary>
/// A skill that can be required or possessed in the IndiaWorks taxonomy.
/// Supports hierarchy via ParentId and is versioned through TaxonomyVersion.
/// </summary>
public sealed class Skill
{
    public long Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public long? ParentId { get; private set; }
    public Skill? Parent { get; private set; }
    public long TaxonomyVersionId { get; private set; }
    public TaxonomyVersion? TaxonomyVersion { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Skill() { }

    public static Skill Create(
        string code,
        string name,
        long taxonomyVersionId,
        long? parentId = null,
        string? description = null,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Skill
        {
            Id = default,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ParentId = parentId,
            TaxonomyVersionId = taxonomyVersionId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}
