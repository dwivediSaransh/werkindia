using System.Text.Json.Serialization;
using IndiaWorks.Infrastructure.Persistence.Seed.Models;

namespace IndiaWorks.Infrastructure.Persistence.Seed;

public sealed record GeographySeedDataset(
    [property: JsonPropertyName("rows")]
    IReadOnlyList<GeographySeedRow> Rows
);
