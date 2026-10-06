using System.Text.Json.Serialization;
using IndiaWorks.Infrastructure.Persistence.Seed.Models;

namespace IndiaWorks.Infrastructure.Persistence.Seed;

public sealed record TaxonomySeedRow(
    [property: JsonPropertyName("entityType")] string EntityType,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("parentCode")] string? ParentCode = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("isActive")] bool IsActive = true,
    [property: JsonPropertyName("jobFamilySlug")] string? JobFamilySlug = null,
    [property: JsonPropertyName("canonicalName")] string? CanonicalName = null,
    [property: JsonPropertyName("slug")] string? Slug = null,
    [property: JsonPropertyName("effectiveFrom")] string? EffectiveFrom = null,
    [property: JsonPropertyName("validTo")] string? ValidTo = null,
    [property: JsonPropertyName("alias")] string? Alias = null,
    [property: JsonPropertyName("languageCode")] string? LanguageCode = null,
    [property: JsonPropertyName("regionCode")] string? RegionCode = null,
    [property: JsonPropertyName("aliasType")] string? AliasType = null
);
