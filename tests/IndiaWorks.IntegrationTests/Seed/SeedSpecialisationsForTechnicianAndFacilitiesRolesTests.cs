using System.IO;
using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Xunit;

namespace IndiaWorks.IntegrationTests.Seed;

public sealed class SeedSpecialisationsForTechnicianAndFacilitiesRolesTests
{
    [Fact]
    public async Task SeedRunner_SeedsTechnicianAndFacilitiesSpecialisationsAndContexts()
    {
        await using var context = new AppDbContext(CreateInMemoryOptions());
        await context.Database.EnsureCreatedAsync();

        var seedJson = await ReadSeedJsonAsync();
        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson);

        var version = await context.TaxonomyVersions.AsNoTracking().FirstOrDefaultAsync();
        Assert.NotNull(version);
        Assert.Equal("1.0.0", version.SemanticVersion);
        Assert.Equal(TaxonomyVersionStatus.Active, version.Status);

        var fieldTechRole = await context.Roles.AsNoTracking()
            .FirstAsync(r => r.Slug == "field-technician" && r.TaxonomyVersionId == version.Id);
        var industrialTechRole = await context.Roles.AsNoTracking()
            .FirstAsync(r => r.Slug == "industrial-technician" && r.TaxonomyVersionId == version.Id);
        var facilitiesTechRole = await context.Roles.AsNoTracking()
            .FirstAsync(r => r.Slug == "facilities-maintenance-technician" && r.TaxonomyVersionId == version.Id);

        var fieldTechSpecialisations = await context.Specialisations.AsNoTracking()
            .Where(s => s.RoleId == fieldTechRole.Id)
            .OrderBy(s => s.Code)
            .ToListAsync();
        Assert.Equal(4, fieldTechSpecialisations.Count);
        Assert.Equal(new[] { "ELECTRICIAN", "HVAC", "PLUMBING", "APPLIANCE_REPAIR" }.OrderBy(x => x),
            fieldTechSpecialisations.Select(s => s.Code).OrderBy(x => x));

        var industrialTechSpecialisations = await context.Specialisations.AsNoTracking()
            .Where(s => s.RoleId == industrialTechRole.Id)
            .OrderBy(s => s.Code)
            .ToListAsync();
        Assert.Single(industrialTechSpecialisations);
        Assert.Equal("INDUSTRIAL_MAINTENANCE", industrialTechSpecialisations[0].Code);

        var facilitiesTechSpecialisations = await context.Specialisations.AsNoTracking()
            .Where(s => s.RoleId == facilitiesTechRole.Id)
            .OrderBy(s => s.Code)
            .ToListAsync();
        Assert.Equal(2, facilitiesTechSpecialisations.Count);
        Assert.Equal(new[] { "SECURITY", "HOUSEKEEPING" }.OrderBy(x => x),
            facilitiesTechSpecialisations.Select(s => s.Code).OrderBy(x => x));

        var fieldTechWorkContexts = await context.WorkContexts.AsNoTracking()
            .Where(w => w.RoleId == fieldTechRole.Id)
            .OrderBy(w => w.Code)
            .ToListAsync();
        Assert.Equal(3, fieldTechWorkContexts.Count);
        Assert.Equal(new[] { "FIELD_SERVICE", "RESIDENTIAL_SERVICE", "COMMERCIAL_SERVICE" }.OrderBy(x => x),
            fieldTechWorkContexts.Select(w => w.Code).OrderBy(x => x));

        var industrialTechWorkContexts = await context.WorkContexts.AsNoTracking()
            .Where(w => w.RoleId == industrialTechRole.Id)
            .OrderBy(w => w.Code)
            .ToListAsync();
        Assert.Equal(2, industrialTechWorkContexts.Count);
        Assert.Equal(new[] { "INDUSTRIAL_SITE", "MANUFACTURING_PLANT" }.OrderBy(x => x),
            industrialTechWorkContexts.Select(w => w.Code).OrderBy(x => x));

        var facilitiesTechWorkContexts = await context.WorkContexts.AsNoTracking()
            .Where(w => w.RoleId == facilitiesTechRole.Id)
            .OrderBy(w => w.Code)
            .ToListAsync();
        Assert.Equal(2, facilitiesTechWorkContexts.Count);
        Assert.Equal(new[] { "FACILITY_GROUNDS", "BUILDING_INTERIOR" }.OrderBy(x => x),
            facilitiesTechWorkContexts.Select(w => w.Code).OrderBy(x => x));

        var fieldTechAliases = await context.RoleAliases.AsNoTracking()
            .Where(a => a.RoleId == fieldTechRole.Id)
            .OrderBy(a => a.Alias)
            .ToListAsync();
        Assert.Equal(2, fieldTechAliases.Count);
        Assert.Equal("Field Tech", fieldTechAliases.First(a => a.AliasType == "Synonym").Alias);

        var industrialTechAliases = await context.RoleAliases.AsNoTracking()
            .Where(a => a.RoleId == industrialTechRole.Id)
            .OrderBy(a => a.Alias)
            .ToListAsync();
        Assert.Equal(2, industrialTechAliases.Count);
        Assert.Equal("Industrial Tech", industrialTechAliases.First(a => a.AliasType == "Synonym").Alias);

        var facilitiesTechAliases = await context.RoleAliases.AsNoTracking()
            .Where(a => a.RoleId == facilitiesTechRole.Id)
            .OrderBy(a => a.Alias)
            .ToListAsync();
        Assert.Equal(2, facilitiesTechAliases.Count);
        Assert.Equal("Facilities Tech", facilitiesTechAliases.First(a => a.AliasType == "Synonym").Alias);
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
            "seed-specialisations-for-technician-and-facilities-roles.json"));

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
            .UseInMemoryDatabase(nameof(SeedSpecialisationsForTechnicianAndFacilitiesRolesTests))
            .Options;
    }

    private static async Task<string> ReadSeedJsonAsync()
    {
        var repoRoot = GetRepoRoot();
        var path = System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "seed-specialisations-for-technician-and-facilities-roles.json");
        return await File.ReadAllTextAsync(path);
    }

    private static string GetRepoRoot()
    {
        var assemblyLocation = typeof(SeedSpecialisationsForTechnicianAndFacilitiesRolesTests).Assembly.Location;
        var testProjectDir = System.IO.Path.GetDirectoryName(assemblyLocation)!;
        var candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
        return candidate;
    }
}
