using System;

namespace IndiaWorks.Domain.E03;

/// <summary>
/// A work-context dimension attached to a role (e.g. shift pattern, work-mode flavor,
/// engagement context). Scoped to a role and versioned indirectly through the role.
/// </summary>
public sealed class WorkContext
{
    public long Id { get; private set; }
    public long RoleId { get; private set; }
    public Role? Role { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WorkContext() { }

    public static WorkContext Create(
        long roleId,
        string code,
        string name,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new WorkContext
        {
            Id = default,
            RoleId = roleId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}
