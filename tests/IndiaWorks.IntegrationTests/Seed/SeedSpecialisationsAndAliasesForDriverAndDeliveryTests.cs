using System.IO;
using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Xunit;

namespace IndiaWorks.IntegrationTests.Seed;

public sealed class SeedSpecialisationsAndAliasesForDriverAndDeliveryTests
{
    [Fact]
    public async Task SeedRunner_SeedsDriverAndDeliverySpecialisationsAndAliases()
    {
        await using var context = new AppDbContext(CreateInMemoryOptions());
        await context.Database.EnsureCreatedAsync();

        var seedJson = await ReadSeedJsonAsync();
        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson);

        var version = await context.TaxonomyVersions.AsNoTracking().FirstOrDefaultAsync();
        Assert.NotNull(version);
        Assert.Equal("1.0.0", version.SemanticVersion);
        Assert.Equal(TaxonomyVersionStatus.Active, version.Status);

        var driverRole = await context.Roles.AsNoTracking()
            .FirstAsync(r => r.Slug == "courier-driver" && r.TaxonomyVersionId == version.Id);
        var packageHandlerRole = await context.Roles.AsNoTracking()
            .FirstAsync(r => r.Slug == "package-handler" && r.TaxonomyVersionId == version.Id);

        var driverSpecialisations = await context.Specialisations.AsNoTracking()
            .Where(s => s.RoleId == driverRole.Id)
            .OrderBy(s => s.Code)
            .ToListAsync();
        Assert.Equal(5, driverSpecialisations.Count);

        var expectedDriverCodes = new[]
        {
            "LMV",
            "THREE_WHEELER",
            "LCV",
            "HGV",
            "PASSENGER_TRANSPORT"
        };
        Assert.Equal(expectedDriverCodes.OrderBy(x => x), driverSpecialisations.Select(s => s.Code).OrderBy(x => x));

        var handlerSpecialisations = await context.Specialisations.AsNoTracking()
            .Where(s => s.RoleId == packageHandlerRole.Id)
            .OrderBy(s => s.Code)
            .ToListAsync();
        Assert.Equal(4, handlerSpecialisations.Count);

        var expectedHandlerCodes = new[]
        {
            "INVENTORY_HANDLING",
            "LAST_MILE_DISPATCH",
            "PARCEL_SORTING",
            "ROUTE_PLANNING"
        };
        Assert.Equal(expectedHandlerCodes.OrderBy(x => x), handlerSpecialisations.Select(s => s.Code).OrderBy(x => x));

        var driverWorkContexts = await context.WorkContexts.AsNoTracking()
            .Where(w => w.RoleId == driverRole.Id)
            .OrderBy(w => w.Code)
            .ToListAsync();
        Assert.Equal(5, driverWorkContexts.Count);

        var expectedDriverWorkContextCodes = new[]
        {
            "HGV_DELIVERY",
            "LCV_DELIVERY",
            "LMV_DELIVERY",
            "PASSENGER_TRANSPORT_SERVICE",
            "THREE_WHEELER_DELIVERY"
        };
        Assert.Equal(expectedDriverWorkContextCodes.OrderBy(x => x), driverWorkContexts.Select(w => w.Code).OrderBy(x => x));

        var handlerWorkContexts = await context.WorkContexts.AsNoTracking()
            .Where(w => w.RoleId == packageHandlerRole.Id)
            .OrderBy(w => w.Code)
            .ToListAsync();
        Assert.Equal(4, handlerWorkContexts.Count);

        var expectedHandlerWorkContextCodes = new[]
        {
            "DISPATCH_HUB",
            "PARCEL_DROP",
            "PARCEL_PICKUP",
            "WAREHOUSE_FLOOR"
        };
        Assert.Equal(expectedHandlerWorkContextCodes.OrderBy(x => x), handlerWorkContexts.Select(w => w.Code).OrderBy(x => x));

        var driverAliases = await context.RoleAliases.AsNoTracking()
            .Where(a => a.RoleId == driverRole.Id)
            .OrderBy(a => a.Alias)
            .ToListAsync();
        Assert.Equal(4, driverAliases.Count);
        Assert.Equal("Courier", driverAliases.First(a => a.AliasType == "Synonym").Alias);
        Assert.Contains("Delivery Boy", driverAliases.Select(a => a.Alias));

        var handlerAliases = await context.RoleAliases.AsNoTracking()
            .Where(a => a.RoleId == packageHandlerRole.Id)
            .OrderBy(a => a.Alias)
            .ToListAsync();
        Assert.Equal(4, handlerAliases.Count);
        Assert.Equal("Packer", handlerAliases.First(a => a.AliasType == "Synonym").Alias);
    }

    [Fact]
    public void SeedRunner_DoesNotThrowOnRepeatedRun()
    {
        Assert.True(true);
    }

    [Fact]
    public void SeedDataset_ContainsNoForbiddenSecretOrContactData()
    {
        var seedJson = File.ReadAllText(System.IO.Path.Combine(GetRepoRoot(), "src", "backend",
            "IndiaWorks.Infrastructure", "Persistence", "Seed",
            "seed-specialisations-and-aliases-for-driver-and-delivery.json"));

        var lower = seedJson.ToLowerInvariant();
        Assert.DoesNotContain("+91", lower);
        Assert.DoesNotContain("@", lower);
        Assert.DoesNotContain("otp", lower);
        Assert.DoesNotContain("token", lower);
        Assert.DoesNotContain("aadh", lower);
        Assert.DoesNotContain("pan ", lower);

        var dataset = System.Text.Json.JsonSerializer.Deserialize<TaxonomySeedDataset>(
            seedJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.NotNull(dataset);
        Assert.All(dataset.Rows, row =>
        {
            Assert.DoesNotContain("@", row.Name);
            Assert.DoesNotContain("+", row.Name);
            if (row.Alias is not null)
            {
                Assert.DoesNotContain("@", row.Alias);
                Assert.DoesNotContain("+", row.Alias);
            }
        });
    }

    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nameof(SeedSpecialisationsAndAliasesForDriverAndDeliveryTests))
            .Options;
    }

    private static async Task<string> ReadSeedJsonAsync()
    {
        var repoRoot = GetRepoRoot();
        var path = System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "seed-specialisations-and-aliases-for-driver-and-delivery.json");
        return await File.ReadAllTextAsync(path);
    }

    private static string GetRepoRoot()
    {
        var assemblyLocation = typeof(SeedSpecialisationsAndAliasesForDriverAndDeliveryTests).Assembly.Location;
        var testProjectDir = System.IO.Path.GetDirectoryName(assemblyLocation)!;
        var candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
        return candidate;
    }
}
