using System.Text.Json.Serialization;
using IndiaWorks.Infrastructure.Persistence.Seed.Models;

namespace IndiaWorks.Infrastructure.Persistence.Seed;

public sealed record PlanSeedDataset(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("semanticVersion")] string SemanticVersion,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("effectiveFrom")] string EffectiveFrom,
    [property: JsonPropertyName("rows")] IReadOnlyList<PlanSeedRow> Rows,
    [property: JsonPropertyName("releaseNotes")] string? ReleaseNotes = null
);