using System;

namespace IndiaWorks.Domain.E03;

/// <summary>
/// A canonical role within a job family and taxonomy version (e.g. "Delivery Driver", "Registered Nurse").
/// Identified by a stable slug within its version.
/// </summary>
public sealed class Role
{
    public long Id { get; private set; }
    public string CanonicalName { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public long JobFamilyId { get; private set; }
    public JobFamily? JobFamily { get; private set; }
    public long TaxonomyVersionId { get; private set; }
    public TaxonomyVersion? TaxonomyVersion { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Role() { }

    public static Role Create(
        string canonicalName,
        string slug,
        long jobFamilyId,
        long taxonomyVersionId,
        bool isActive = true,
        DateTime? validFrom = null,
        DateTime? validTo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Role
        {
            Id = default,
            CanonicalName = canonicalName.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            JobFamilyId = jobFamilyId,
            TaxonomyVersionId = taxonomyVersionId,
            IsActive = isActive,
            ValidFrom = validFrom ?? DateTime.UtcNow,
            ValidTo = validTo,
            CreatedAt = DateTime.UtcNow
        };
    }
}
