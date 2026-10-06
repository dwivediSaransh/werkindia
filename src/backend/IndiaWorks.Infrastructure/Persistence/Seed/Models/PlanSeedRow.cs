using System.Text.Json.Serialization;
using IndiaWorks.Infrastructure.Persistence.Seed.Models;

namespace IndiaWorks.Infrastructure.Persistence.Seed;

public sealed record PlanSeedRow(
    [property: JsonPropertyName("entityType")] string EntityType,
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("isActive")] bool IsActive = true,
    [property: JsonPropertyName("unit")] string? Unit = null,
    [property: JsonPropertyName("resetCadence")] string? ResetCadence = null,
    [property: JsonPropertyName("planCode")] string? PlanCode = null,
    [property: JsonPropertyName("interval")] string? Interval = null,
    [property: JsonPropertyName("currency")] string? Currency = null,
    [property: JsonPropertyName("amount")] decimal? Amount = null,
    [property: JsonPropertyName("trialDays")] int? TrialDays = null,
    [property: JsonPropertyName("entitlementCode")] string? EntitlementCode = null,
    [property: JsonPropertyName("enabled")] bool Enabled = true,
    [property: JsonPropertyName("limit")] decimal? Limit = null
);