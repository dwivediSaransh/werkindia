namespace IndiaWorks.Domain.E04;

/// <summary>
/// Catalog entry defining a reusable entitlement type (e.g. candidate_views, job_slots).
/// </summary>
public sealed class EntitlementDefinition
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public EntitlementUnit Unit { get; private set; }
    public ResetCadence ResetCadence { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EntitlementDefinition() { }

    public static EntitlementDefinition Create(string code, string name, EntitlementUnit unit, ResetCadence resetCadence, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new EntitlementDefinition
        {
            Code = code.Trim(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Unit = unit,
            ResetCadence = resetCadence,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }
}