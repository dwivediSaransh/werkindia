using System;

namespace IndiaWorks.Domain.E03;

/// <summary>
/// A credential type that can be required or held (e.g. degree, license, certification).
/// Versioned through TaxonomyVersion.
/// </summary>
public sealed class CredentialType
{
    public long Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public long TaxonomyVersionId { get; private set; }
    public TaxonomyVersion? TaxonomyVersion { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CredentialType() { }

    public static CredentialType Create(
        string code,
        string name,
        long taxonomyVersionId,
        string? description = null,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new CredentialType
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
