using System;

namespace IndiaWorks.Domain.E03;

/// <summary>
/// An alternative or historical name for a role, used for search/alias resolution.
/// </summary>
public sealed class RoleAlias
{
    public long Id { get; private set; }
    public long RoleId { get; private set; }
    public Role? Role { get; private set; }
    public string Alias { get; private set; } = default!;
    public string? LanguageCode { get; private set; }
    public string? RegionCode { get; private set; }
    public string? AliasType { get; private set; }
    public decimal? Weight { get; private set; }
    public bool IsDeprecated { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RoleAlias() { }

    public static RoleAlias Create(
        long roleId,
        string alias,
        string? languageCode = null,
        string? regionCode = null,
        string? aliasType = null,
        decimal? weight = null,
        bool isDeprecated = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);

        return new RoleAlias
        {
            Id = default,
            RoleId = roleId,
            Alias = alias.Trim(),
            LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim().ToUpperInvariant(),
            RegionCode = string.IsNullOrWhiteSpace(regionCode) ? null : regionCode.Trim().ToUpperInvariant(),
            AliasType = string.IsNullOrWhiteSpace(aliasType) ? null : aliasType.Trim(),
            Weight = weight,
            IsDeprecated = isDeprecated,
            CreatedAt = DateTime.UtcNow
        };
    }
}
