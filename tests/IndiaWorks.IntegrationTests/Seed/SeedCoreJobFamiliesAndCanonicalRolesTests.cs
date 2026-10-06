using System.IO;
using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Xunit;
using IndiaWorks.IntegrationTests.Database;

namespace IndiaWorks.IntegrationTests.Seed;

public sealed class SeedCoreJobFamiliesAndCanonicalRolesTests
{
    [Fact]
    public async Task SeedRunner_SeedsCanonicalJobFamiliesAndRoles_IntegrationTest()
    {
        await using var context = new AppDbContext(CreateInMemoryOptions());
        await context.Database.EnsureCreatedAsync();

        var seedJson = await ReadSeedJsonAsync();

        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson);

        var version = await context.TaxonomyVersions.AsNoTracking().FirstOrDefaultAsync();
        Assert.NotNull(version);
        Assert.Equal("1.0.0", version.SemanticVersion);
        Assert.Equal(TaxonomyVersionStatus.Active, version.Status);

        var jobFamilyCodes = new[]
        {
            "DELIVERY",
            "TECHNICIAN",
            "FACILITIES",
            "PROFESSIONAL",
            "HEALTHCARE"
        };

        var loadedJobFamilies = await context.JobFamilies.AsNoTracking()
            .Where(j => jobFamilyCodes.Contains(j.Code))
            .ToListAsync();

        Assert.Equal(5, loadedJobFamilies.Count);

        var deliveryFamily = loadedJobFamilies.First(j => j.Code == "DELIVERY");
        Assert.Equal(version.Id, deliveryFamily.TaxonomyVersionId);

        var roleSlugs = new[]
        {
            "courier-driver",
            "package-handler",
            "field-technician",
            "industrial-technician",
            "facilities-maintenance-technician",
            "professional-consultant",
            "registered-nurse"
        };

        var loadedRoles = await context.Roles.AsNoTracking()
            .Where(r => roleSlugs.Contains(r.Slug))
            .ToListAsync();

        Assert.Equal(7, loadedRoles.Count);

        var courierDriver = loadedRoles.First(r => r.Slug == "courier-driver");
        Assert.Equal("Courier Driver", courierDriver.CanonicalName);
        Assert.Equal(deliveryFamily.Id, courierDriver.JobFamilyId);
        Assert.Equal(version.Id, courierDriver.TaxonomyVersionId);

        var registeredNurse = loadedRoles.First(r => r.Slug == "registered-nurse");
        var healthcareFamily = loadedJobFamilies.First(j => j.Code == "HEALTHCARE");
        Assert.Equal("Registered Nurse", registeredNurse.CanonicalName);
        Assert.Equal(healthcareFamily.Id, registeredNurse.JobFamilyId);
        Assert.Equal(version.Id, registeredNurse.TaxonomyVersionId);
    }

    [Fact]
    public void SeedDataset_ContainsNoForbiddenSecretOrContactData()
    {
        var seedJson = File.ReadAllText(System.IO.Path.Combine(GetRepoRoot(), "src", "backend",
            "IndiaWorks.Infrastructure", "Persistence", "Seed",
            "seed-core-job-families-and-canonical-roles.json"));

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
        });
    }

    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nameof(SeedCoreJobFamiliesAndCanonicalRolesTests))
            .Options;
    }

    private static async Task<string> ReadSeedJsonAsync()
    {
        var repoRoot = GetRepoRoot();
        var path = System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "seed-core-job-families-and-canonical-roles.json");
        return await File.ReadAllTextAsync(path);
    }

    private static string GetRepoRoot()
    {
        var assemblyLocation = typeof(SeedCoreJobFamiliesAndCanonicalRolesTests).Assembly.Location;
        var testProjectDir = System.IO.Path.GetDirectoryName(assemblyLocation)!;
        var candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
        return candidate;
    }
}
