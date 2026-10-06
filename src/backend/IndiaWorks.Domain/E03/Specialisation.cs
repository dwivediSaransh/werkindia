using System;

namespace IndiaWorks.Domain.E03;

/// <summary>
/// A specialization within a role (e.g. "Neonatal Nursing" under "Registered Nurse").
/// Supports hierarchy and is scoped to a role.
/// </summary>
public sealed class Specialisation
{
    public long Id { get; private set; }
    public long RoleId { get; private set; }
    public Role? Role { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public long? ParentId { get; private set; }
    public Specialisation? Parent { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Specialisation() { }

    public static Specialisation Create(
        long roleId,
        string name,
        string code,
        long? parentId = null,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return new Specialisation
        {
            Id = default,
            RoleId = roleId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            ParentId = parentId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}
