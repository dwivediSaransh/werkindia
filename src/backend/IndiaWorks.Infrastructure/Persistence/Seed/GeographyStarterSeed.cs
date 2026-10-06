using System.Text.Json;
using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed.Models;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Seed;

public static partial class GeographyStarterSeed
{
    public static async Task SeedAsync(
        AppDbContext db,
        string seedJson,
        CancellationToken ct = default)
    {
        var rows = JsonSerializer.Deserialize<GeographySeedDataset>(seedJson, GeoJsonOptions);

        if (rows is null)
        {
            throw new SeedDataException("Seed dataset could not be deserialized.");
        }

        var codeToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        Geography? ResolveByCode(string? code) =>
            code is null ? null : db.Geographies.AsNoTracking().FirstOrDefault(g => g.Code == code);

        Geography CreateGeography(GeographySeedRow row, Guid? parentId)
        {
            var type = row.GeographyType;
            var coords = (row.Latitude, row.Longitude);

            return coords == (null, null)
                ? Geography.Create(type, row.Name, row.Code, parentId)
                : Geography.CreateWithCoordinates(type, row.Name, row.Code, parentId, coords.Latitude, coords.Longitude);
        }

        var byLevel = rows.Rows
            .GroupBy(row => row.GeographyType)
            .ToDictionary(g => g.Key, g => g.AsEnumerable(), GeographyTypeComparer.Instance);

        foreach (var level in Enum.GetValues<GeographyType>())
        {
            if (!byLevel.TryGetValue(level, out var levelRows))
            {
                continue;
            }

            await using var levelTransaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                foreach (var row in levelRows)
                {
                    var existing = ResolveByCode(row.Code);
                    if (existing is not null)
                    {
                        codeToId[row.Code] = existing.Id;
                        continue;
                    }

                    var parentId = row.ParentCode is null ? null : ResolveByCode(row.ParentCode)?.Id;
                    var geography = CreateGeography(row, parentId);

                    db.Geographies.Add(geography);
                    await db.SaveChangesAsync(ct);
                    codeToId[row.Code] = geography.Id;
                }

                await levelTransaction.CommitAsync(ct);
            }
            catch
            {
                await levelTransaction.RollbackAsync(ct);
                throw;
            }
        }
    }

    private static readonly JsonSerializerOptions GeoJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

internal sealed class GeographyTypeComparer : IEqualityComparer<GeographyType>
{
    public static readonly GeographyTypeComparer Instance = new();

    public bool Equals(GeographyType x, GeographyType y) => x == y;
    public int GetHashCode(GeographyType obj) => obj.GetHashCode();
}

