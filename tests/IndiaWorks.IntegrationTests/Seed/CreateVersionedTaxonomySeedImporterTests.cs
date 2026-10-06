using System.IO;
using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Xunit;
using IndiaWorks.IntegrationTests.Database;

namespace IndiaWorks.IntegrationTests.Seed;

public sealed class CreateVersionedTaxonomySeedImporterTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public CreateVersionedTaxonomySeedImporterTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task FreshDatabase_SeedRunner_PersistsTaxonomyVersionAndCodeEntities()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var seedJson = await ReadSeedJsonAsync();

        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson);

        var version = await context.TaxonomyVersions.AsNoTracking().FirstOrDefaultAsync();
        Assert.NotNull(version);
        Assert.Equal("1.0.0", version.SemanticVersion);
        Assert.Equal(TaxonomyVersionStatus.Draft, version.Status);

        var jobFamilyCount = await context.JobFamilies.CountAsync();
        Assert.Equal(2, jobFamilyCount);

        var roleCount = await context.Roles.CountAsync();
        Assert.Equal(2, roleCount);

        var skillCount = await context.Skills.CountAsync();
        Assert.Equal(1, skillCount);

        var credentialTypeCount = await context.CredentialTypes.CountAsync();
        Assert.Equal(1, credentialTypeCount);

        var assetTypeCount = await context.AssetTypes.CountAsync();
        Assert.Equal(1, assetTypeCount);

        var roleAliasCount = await context.RoleAliases.CountAsync();
        Assert.Equal(1, roleAliasCount);

        var deliveryJobFamily = await context.JobFamilies.AsNoTracking()
            .FirstAsync(j => j.Code == "DELIVERY");
        Assert.Equal(version.Id, deliveryJobFamily.TaxonomyVersionId);

        var driverRole = await context.Roles.AsNoTracking()
            .FirstAsync(r => r.Slug == "delivery-driver");
        Assert.Equal(deliveryJobFamily.Id, driverRole.JobFamilyId);
        Assert.Equal(version.Id, driverRole.TaxonomyVersionId);
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task SeedRunner_IsIdempotent_WhenRunTwice()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var seedJson = await ReadSeedJsonAsync();

        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson);
        var firstCount = await context.TaxonomyVersions.CountAsync();
        var firstJobFamilyCount = await context.JobFamilies.CountAsync();
        var firstRoleCount = await context.Roles.CountAsync();

        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson);
        var secondCount = await context.TaxonomyVersions.CountAsync();
        var secondJobFamilyCount = await context.JobFamilies.CountAsync();
        var secondRoleCount = await context.Roles.CountAsync();

        Assert.Equal(firstCount, secondCount);
        Assert.Equal(firstJobFamilyCount, secondJobFamilyCount);
        Assert.Equal(firstRoleCount, secondRoleCount);
    }

    [Fact]
    public async Task SeedRunner_RejectsInvalidEntityType()
    {
        var seedJson = """
        {
          "version": "1.0.0",
          "semanticVersion": "1.0.0",
          "status": "Draft",
          "effectiveFrom": "2026-01-01T00:00:00Z",
          "rows": [
            {
              "entityType": "UnknownEntity",
              "code": "X",
              "name": "X"
            }
          ]
        }
        """;

        await using var context = new AppDbContext(CreateInMemoryOptions());
        await Assert.ThrowsAsync<SeedDataException>(() =>
            CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson));
    }

    [Fact]
    public async Task SeedRunner_RejectsUnknownTaxonomyVersionStatus()
    {
        var seedJson = """
        {
          "version": "1.0.0",
          "semanticVersion": "1.0.0",
          "status": "UnknownStatus",
          "effectiveFrom": "2026-01-01T00:00:00Z",
          "rows": []
        }
        """;

        await using var context = new AppDbContext(CreateInMemoryOptions());
        await Assert.ThrowsAsync<SeedDataException>(() =>
            CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson));
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task FreshDatabase_SeedRunner_DoesNotDuplicateCodesAcrossRuns()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var seedJson = await ReadSeedJsonAsync();

        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson);
        var firstCount = await context.JobFamilies.CountAsync();

        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson);
        var secondCount = await context.JobFamilies.CountAsync();

        Assert.Equal(firstCount, secondCount);
    }

    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nameof(CreateVersionedTaxonomySeedImporterTests))
            .Options;
    }

    private static async Task<string> ReadSeedJsonAsync()
    {
        var repoRoot = GetRepoRoot();
        var path = System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "create-versioned-taxonomy-seed-importer.json");
        return await File.ReadAllTextAsync(path);
    }

    private static string GetRepoRoot()
    {
        var assemblyLocation = typeof(CreateVersionedTaxonomySeedImporterTests).Assembly.Location;
        var testProjectDir = System.IO.Path.GetDirectoryName(assemblyLocation)!;
        var candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
        return candidate;
    }

    [Fact]
    public void SeedDataset_ContainsNoForbiddenSecretOrContactData()
    {
        var seedJson = File.ReadAllText(System.IO.Path.Combine(GetRepoRoot(), "src", "backend",
            "IndiaWorks.Infrastructure", "Persistence", "Seed",
            "create-versioned-taxonomy-seed-importer.json"));

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
}
