using System.Text.Json.Serialization;
using IndiaWorks.Domain.E03;

namespace IndiaWorks.Infrastructure.Persistence.Seed.Models;

public sealed record GeographySeedRow(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("parentCode")] string? ParentCode = null,
    [property: JsonPropertyName("latitude")] decimal? Latitude = null,
    [property: JsonPropertyName("longitude")] decimal? Longitude = null
)
{
    public GeographyType GeographyType { get; init; } =
        Enum.TryParse<GeographyType>(Type, ignoreCase: true, out var t) ? t : throw new SeedDataException($"Unknown GeographyType '{Type}'.");
}
