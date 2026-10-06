using System;
using System.Collections.Generic;

namespace IndiaWorks.Domain.E03;

public sealed class Geography
{
    public Guid Id { get; private set; }
    public GeographyType Type { get; private set; }
    public string? Code { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid? ParentId { get; private set; }
    public Geography? Parent { get; private set; }
    public IReadOnlyCollection<Geography> Children => _children.AsReadOnly();
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<Geography> _children = new();

    private Geography() { }

    public static Geography Create(
        GeographyType type,
        string name,
        string? code = null,
        Guid? parentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Geography
        {
            Id = Guid.NewGuid(),
            Type = type,
            Name = name.Trim(),
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            ParentId = parentId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Geography CreateWithCoordinates(
        GeographyType type,
        string name,
        string? code,
        Guid? parentId,
        decimal? latitude,
        decimal? longitude)
    {
        var geography = Create(type, name, code, parentId);

        geography.Latitude = latitude;
        geography.Longitude = longitude;

        return geography;
    }

    public void ReparentTo(Guid? newParentId)
    {
        if (newParentId == Id)
        {
            throw new ArgumentException("A geography cannot be its own parent.", nameof(newParentId));
        }

        ParentId = newParentId;
    }
}
