using System.IO;
using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;
using IndiaWorks.IntegrationTests.Database;

namespace IndiaWorks.IntegrationTests.Seed;

public sealed class GeographyStarterSeedTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public GeographyStarterSeedTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task FreshDatabase_SeedRunner_PersistsRepresentativeIndiaGeography()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var seedJson = await ReadSeedJsonAsync();

        await GeographyStarterSeed.SeedAsync(context, seedJson);

        var count = await context.Geographies.CountAsync();
        Assert.Equal(24, count);

        var india = await context.Geographies.AsNoTracking().FirstAsync(g => g.Code == "IN");
        Assert.Equal(GeographyType.Country, india.Type);
        Assert.Equal("India", india.Name);

        var karnataka = await context.Geographies.AsNoTracking().FirstAsync(g => g.Code == "KA");
        Assert.Equal(GeographyType.State, karnataka.Type);
        Assert.Equal(india.Id, karnataka.ParentId);

        var bengaluru = await context.Geographies.AsNoTracking().FirstAsync(g => g.Code == "BLR");
        Assert.Equal(GeographyType.City, bengaluru.Type);
        Assert.Equal(12.9716m, bengaluru.Latitude);
        Assert.Equal(77.5946m, bengaluru.Longitude);

        var indiranagar = await context.Geographies.AsNoTracking().FirstAsync(g => g.Code == "INDIRA_NAGAR");
        Assert.Equal(GeographyType.Locality, indiranagar.Type);
        Assert.Equal(bengaluru.Id, indiranagar.ParentId);

        var blrPin = await context.Geographies.AsNoTracking().FirstAsync(g => g.Code == "560038");
        Assert.Equal(GeographyType.Pin, blrPin.Type);
        Assert.Equal(indiranagar.Id, blrPin.ParentId);

        var rangaReddy = await context.Geographies.AsNoTracking().FirstAsync(g => g.Code == "RANGAREDDY");
        Assert.Equal(GeographyType.District, rangaReddy.Type);

        var parentChain = new List<Geography>();
        Geography? current = blrPin;
        while (current is not null)
        {
            parentChain.Insert(0, current);
            current = current.ParentId is null
                ? null
                : await context.Geographies.AsNoTracking().FirstAsync(g => g.Id == current!.ParentId);
        }

        Assert.Equal(5, parentChain.Count);
        Assert.Equal("IN", parentChain[0].Code);
        Assert.Equal("KA", parentChain[1].Code);
        Assert.Equal("BLR", parentChain[2].Code);
        Assert.Equal("INDIRA_NAGAR", parentChain[3].Code);
        Assert.Equal("560038", parentChain[4].Code);
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task SeedRunner_IsIdempotent_WhenRunTwice()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var seedJson = await ReadSeedJsonAsync();

        await GeographyStarterSeed.SeedAsync(context, seedJson);
        var firstCount = await context.Geographies.CountAsync();

        await GeographyStarterSeed.SeedAsync(context, seedJson);
        var secondCount = await context.Geographies.CountAsync();

        Assert.Equal(firstCount, secondCount);
        Assert.Equal(24, secondCount);
    }

    private static async Task<string> ReadSeedJsonAsync()
    {
        var repoRoot = GetRepoRoot();
        var path = System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure", "Persistence", "Seed", "seed-india-geography-starter-dataset-for-development.json");
        return await File.ReadAllTextAsync(path);
    }

    private static string GetRepoRoot()
    {
        var assemblyLocation = typeof(GeographyStarterSeedTests).Assembly.Location;
        var testProjectDir = System.IO.Path.GetDirectoryName(assemblyLocation)!;
        var candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
        return candidate;
    }

    [Fact]
    public void SeedDataset_ContainsNoForbiddenSecretOrContactData()
    {
        var seedJson = File.ReadAllText(System.IO.Path.Combine(GetRepoRoot(), "src", "backend", "IndiaWorks.Infrastructure", "Persistence", "Seed", "seed-india-geography-starter-dataset-for-development.json"));

        var lower = seedJson.ToLowerInvariant();
        Assert.DoesNotContain("+91", lower);
        Assert.DoesNotContain("@", lower);
        Assert.DoesNotContain("otp", lower);
        Assert.DoesNotContain("token", lower);
        Assert.DoesNotContain("aadh", lower);
        Assert.DoesNotContain("pan ", lower);

        var dataset = System.Text.Json.JsonSerializer.Deserialize<GeographySeedDataset>(
            seedJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.NotNull(dataset);
        Assert.All(dataset.Rows, row =>
        {
            Assert.DoesNotContain("@", row.Name);
            Assert.DoesNotContain("+", row.Name);
        });
    }
}
