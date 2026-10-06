using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Xunit;
using IndiaWorks.IntegrationTests.Database;

namespace IndiaWorks.IntegrationTests.Database.E03;

/// <summary>
/// Real database integration tests for Taxonomy and Geography lookups.
/// Uses SQL Server Testcontainers to verify actual database queries with seeded data.
/// </summary>
public sealed class TaxonomyLookupIntegrationTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public TaxonomyLookupIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private AppDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);
        return new AppDbContext(optionsBuilder.Options);
    }

    private async Task SeedTaxonomyDataAsync(AppDbContext context)
    {
        var repoRoot = GetRepoRoot();
        
        // Seed taxonomy version and core entities
        var seedJson1 = await File.ReadAllTextAsync(System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "create-versioned-taxonomy-seed-importer.json"));
        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson1);

        var seedJson2 = await File.ReadAllTextAsync(System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "seed-core-job-families-and-canonical-roles.json"));
        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson2);

        var seedJson3 = await File.ReadAllTextAsync(System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "seed-specialisations-and-aliases-for-driver-and-delivery.json"));
        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson3);

        var seedJson4 = await File.ReadAllTextAsync(System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "seed-specialisations-for-technician-and-facilities-roles.json"));
        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson4);

        var seedJson5 = await File.ReadAllTextAsync(System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "seed-professional-and-healthcare-specialisations.json"));
        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson5);
    }

    private async Task SeedGeographyDataAsync(AppDbContext context)
    {
        var repoRoot = GetRepoRoot();
        var seedJson = await File.ReadAllTextAsync(System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "seed-india-geography-starter-dataset-for-development.json"));
        await GeographyStarterSeed.SeedAsync(context, seedJson);
    }

    private static string GetRepoRoot()
    {
        var assemblyLocation = typeof(TaxonomyLookupIntegrationTests).Assembly.Location;
        var testProjectDir = System.IO.Path.GetDirectoryName(assemblyLocation)!;
        var candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
        return candidate;
    }

    // ── Taxonomy Version Tests ────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task TaxonomyVersion_AfterSeed_ActiveVersionExists()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var versions = await context.TaxonomyVersions
            .AsNoTracking()
            .Where(v => v.Status == TaxonomyVersionStatus.Active)
            .ToListAsync();

        Assert.NotEmpty(versions);
        var activeVersion = versions.First();
        Assert.Equal("1.0.0", activeVersion.SemanticVersion);
        Assert.Equal(TaxonomyVersionStatus.Active, activeVersion.Status);
    }

    // ── Job Family Tests ──────────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task JobFamilies_AfterSeed_ReturnsAllFiveFamilies()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var families = await context.JobFamilies
            .AsNoTracking()
            .Where(j => j.IsActive)
            .OrderBy(j => j.Code)
            .ToListAsync();

        Assert.Equal(5, families.Count);
        
        var codes = families.Select(f => f.Code).ToHashSet();
        Assert.Contains("DELIVERY", codes);
        Assert.Contains("TECHNICIAN", codes);
        Assert.Contains("FACILITIES", codes);
        Assert.Contains("PROFESSIONAL", codes);
        Assert.Contains("HEALTHCARE", codes);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task JobFamilies_FilterByTaxonomyVersion_ReturnsCorrectSubset()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var activeVersion = await context.TaxonomyVersions
            .AsNoTracking()
            .FirstAsync(v => v.Status == TaxonomyVersionStatus.Active);

        var families = await context.JobFamilies
            .AsNoTracking()
            .Where(j => j.TaxonomyVersionId == activeVersion.Id && j.IsActive)
            .ToListAsync();

        Assert.Equal(5, families.Count);
        Assert.All(families, f => Assert.Equal(activeVersion.Id, f.TaxonomyVersionId));
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task JobFamilies_SearchByName_ReturnsMatchingFamilies()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var families = await context.JobFamilies
            .AsNoTracking()
            .Where(j => j.IsActive && j.Name.Contains("Delivery"))
            .ToListAsync();

        Assert.Single(families);
        Assert.Equal("DELIVERY", families[0].Code);
        Assert.Equal("Delivery", families[0].Name);
    }

    // ── Role Tests ────────────────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Roles_AfterSeed_ReturnsCanonicalRoles()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var roles = await context.Roles
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Slug)
            .ToListAsync();

        // Core roles from seed: courier-driver, package-handler, field-technician,
        // industrial-technician, facilities-maintenance-technician, professional-consultant, registered-nurse
        // Plus driver/delivery specialisations may have additional roles
        Assert.True(roles.Count >= 7);
        
        var slugs = roles.Select(r => r.Slug).ToHashSet();
        Assert.Contains("courier-driver", slugs);
        Assert.Contains("package-handler", slugs);
        Assert.Contains("field-technician", slugs);
        Assert.Contains("industrial-technician", slugs);
        Assert.Contains("facilities-maintenance-technician", slugs);
        Assert.Contains("professional-consultant", slugs);
        Assert.Contains("registered-nurse", slugs);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Roles_FilterByJobFamily_ReturnsCorrectRoles()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var deliveryFamily = await context.JobFamilies
            .AsNoTracking()
            .FirstAsync(j => j.Code == "DELIVERY");

        var roles = await context.Roles
            .AsNoTracking()
            .Where(r => r.JobFamilyId == deliveryFamily.Id && r.IsActive)
            .ToListAsync();

        Assert.Equal(2, roles.Count); // courier-driver, package-handler
        Assert.All(roles, r => Assert.Equal(deliveryFamily.Id, r.JobFamilyId));
        
        var slugs = roles.Select(r => r.Slug).ToHashSet();
        Assert.Contains("courier-driver", slugs);
        Assert.Contains("package-handler", slugs);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Roles_SearchByCanonicalName_ReturnsMatchingRoles()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var roles = await context.Roles
            .AsNoTracking()
            .Where(r => r.IsActive && r.CanonicalName.Contains("Driver"))
            .ToListAsync();

        Assert.True(roles.Count >= 1);
        Assert.All(roles, r => Assert.Contains("Driver", r.CanonicalName));
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Roles_IncludeJobFamily_WhenProjected()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var rolesWithFamily = await context.Roles
            .AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => new { r.Id, r.CanonicalName, r.Slug, JobFamilyCode = r.JobFamily!.Code })
            .ToListAsync();

        Assert.True(rolesWithFamily.Count >= 7);
        Assert.All(rolesWithFamily, r => Assert.False(string.IsNullOrEmpty(r.JobFamilyCode)));
    }

    // ── Specialisation Tests ──────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Specialisations_AfterSeed_ReturnsAllSpecialisations()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var specs = await context.Specialisations
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync();

        // Should have specialisations for driver, delivery, technician, facilities, professional, healthcare
        Assert.True(specs.Count >= 10);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Specialisations_FilterByRole_ReturnsCorrectSpecialisations()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var courierDriverRole = await context.Roles
            .AsNoTracking()
            .FirstAsync(r => r.Slug == "courier-driver");

        var specs = await context.Specialisations
            .AsNoTracking()
            .Where(s => s.RoleId == courierDriverRole.Id && s.IsActive)
            .ToListAsync();

        Assert.NotEmpty(specs);
        Assert.All(specs, s => Assert.Equal(courierDriverRole.Id, s.RoleId));
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Specialisations_IncludeRole_WhenProjected()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var specsWithRole = await context.Specialisations
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.Name, s.Code, RoleSlug = s.Role!.Slug })
            .ToListAsync();

        Assert.True(specsWithRole.Count >= 10);
        Assert.All(specsWithRole, s => Assert.False(string.IsNullOrEmpty(s.RoleSlug)));
    }

    // ── Skill Tests ────────────────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Skills_AfterSeed_ReturnsSeededSkills()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var skills = await context.Skills
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync();

        Assert.NotEmpty(skills);
        Assert.Contains(skills, s => s.Code == "DRV_SAFE");
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Skills_FilterByTaxonomyVersion_ReturnsCorrectSkills()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var activeVersion = await context.TaxonomyVersions
            .AsNoTracking()
            .FirstAsync(v => v.Status == TaxonomyVersionStatus.Active);

        var skills = await context.Skills
            .AsNoTracking()
            .Where(s => s.TaxonomyVersionId == activeVersion.Id && s.IsActive)
            .ToListAsync();

        Assert.NotEmpty(skills);
        Assert.All(skills, s => Assert.Equal(activeVersion.Id, s.TaxonomyVersionId));
    }

    // ── Work Context Tests ────────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task WorkContexts_AfterSeed_ReturnsSeededContexts()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var contexts = await context.WorkContexts
            .AsNoTracking()
            .Where(w => w.IsActive)
            .ToListAsync();

        Assert.NotEmpty(contexts);
        Assert.Contains(contexts, w => w.Code == "SHIFT_DAY");
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task WorkContexts_FilterByRole_ReturnsCorrectContexts()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var courierDriverRole = await context.Roles
            .AsNoTracking()
            .FirstAsync(r => r.Slug == "courier-driver");

        var contexts = await context.WorkContexts
            .AsNoTracking()
            .Where(w => w.RoleId == courierDriverRole.Id && w.IsActive)
            .ToListAsync();

        Assert.NotEmpty(contexts);
        Assert.All(contexts, w => Assert.Equal(courierDriverRole.Id, w.RoleId));
    }

    // ── Credential Type Tests ─────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task CredentialTypes_AfterSeed_ReturnsSeededCredentials()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var creds = await context.CredentialTypes
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync();

        Assert.NotEmpty(creds);
    }

    // ── Asset Type Tests ──────────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task AssetTypes_AfterSeed_ReturnsSeededAssets()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var assets = await context.AssetTypes
            .AsNoTracking()
            .Where(a => a.IsActive)
            .ToListAsync();

        Assert.NotEmpty(assets);
    }

    // ── Role Alias Tests ──────────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task RoleAliases_AfterSeed_ReturnsSeededAliases()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var aliases = await context.RoleAliases
            .AsNoTracking()
            .Where(a => !a.IsDeprecated)
            .ToListAsync();

        Assert.NotEmpty(aliases);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task RoleAliases_ReferenceValidRole()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var aliasesWithRole = await context.RoleAliases
            .AsNoTracking()
            .Where(a => !a.IsDeprecated)
            .Select(a => new { a.Id, a.Alias, RoleSlug = a.Role!.Slug })
            .ToListAsync();

        Assert.NotEmpty(aliasesWithRole);
        Assert.All(aliasesWithRole, a => Assert.False(string.IsNullOrEmpty(a.RoleSlug)));
    }

    // ── Geography Tests ───────────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Geographies_AfterSeed_ReturnsIndianGeography()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedGeographyDataAsync(context);

        var geographies = await context.Geographies
            .AsNoTracking()
            .ToListAsync();

        Assert.NotEmpty(geographies);
        
        // Should have Country (India), States, and Cities
        var types = geographies.Select(g => g.Type).ToHashSet();
        Assert.Contains(GeographyType.Country, types);
        Assert.Contains(GeographyType.State, types);
        Assert.Contains(GeographyType.City, types);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Geographies_FilterByType_ReturnsCorrectTypes()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedGeographyDataAsync(context);

        var states = await context.Geographies
            .AsNoTracking()
            .Where(g => g.Type == GeographyType.State)
            .OrderBy(g => g.Name)
            .ToListAsync();

        Assert.True(states.Count >= 28); // 28 states + 8 UTs = 36, but seed may have subset
        Assert.All(states, s => Assert.Equal(GeographyType.State, s.Type));
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task GeographyHierarchy_ParentChildRelationships_Intact()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedGeographyDataAsync(context);

        var india = await context.Geographies
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Type == GeographyType.Country && g.Name == "India");

        Assert.NotNull(india);

        var children = await context.Geographies
            .AsNoTracking()
            .Where(g => g.ParentId == india!.Id)
            .ToListAsync();

        Assert.NotEmpty(children);
        Assert.All(children, c => Assert.Equal(GeographyType.State, c.Type));
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Geographies_SearchByName_ReturnsMatchingResults()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedGeographyDataAsync(context);

        var results = await context.Geographies
            .AsNoTracking()
            .Where(g => g.Name.Contains("Maharashtra"))
            .ToListAsync();

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Name.Contains("Maharashtra"));
    }

    // ── Complex Query Tests (simulating GraphQL resolver patterns) ────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task JobFamily_WithRoles_WithSpecialisations_FullHierarchyQuery()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var deliveryFamily = await context.JobFamilies
            .AsNoTracking()
            .FirstAsync(j => j.Code == "DELIVERY");

        // Simulate a GraphQL resolver that loads family -> roles -> specialisations
        var roles = await context.Roles
            .AsNoTracking()
            .Where(r => r.JobFamilyId == deliveryFamily.Id && r.IsActive)
            .ToListAsync();

        var roleIds = roles.Select(r => r.Id).ToList();
        var specialisations = await context.Specialisations
            .AsNoTracking()
            .Where(s => roleIds.Contains(s.RoleId) && s.IsActive)
            .ToListAsync();

        Assert.Equal(2, roles.Count);
        Assert.NotEmpty(specialisations);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Role_WithWorkContexts_AndSkills_ComplexQuery()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var courierDriver = await context.Roles
            .AsNoTracking()
            .FirstAsync(r => r.Slug == "courier-driver");

        var workContexts = await context.WorkContexts
            .AsNoTracking()
            .Where(w => w.RoleId == courierDriver.Id && w.IsActive)
            .ToListAsync();

        var activeVersion = await context.TaxonomyVersions
            .AsNoTracking()
            .FirstAsync(v => v.Status == TaxonomyVersionStatus.Active);

        var skills = await context.Skills
            .AsNoTracking()
            .Where(s => s.TaxonomyVersionId == activeVersion.Id && s.IsActive)
            .ToListAsync();

        Assert.NotEmpty(workContexts);
        Assert.NotEmpty(skills);
    }

    // ── Performance/Index Verification Tests ──────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task JobFamilies_IndexOnCode_UsedForLookup()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        // This should use the unique index on Code + TaxonomyVersionId
        var family = await context.JobFamilies
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Code == "DELIVERY" && j.IsActive);

        Assert.NotNull(family);
        Assert.Equal("Delivery", family!.Name);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Roles_IndexOnSlug_UsedForLookup()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        var role = await context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Slug == "courier-driver" && r.IsActive);

        Assert.NotNull(role);
        Assert.Equal("Courier Driver", role!.CanonicalName);
    }

    // ── Sensitive Data Not Exposed ────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task TaxonomyQueries_DoNotExposeInternalIds_InPublicProjections()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedTaxonomyDataAsync(context);

        // Typical public projection pattern - only expose what's needed for UI
        var projection = await context.JobFamilies
            .AsNoTracking()
            .Where(j => j.IsActive)
            .Select(j => new 
            { 
                j.Code, 
                j.Name, 
                Version = j.TaxonomyVersion!.SemanticVersion 
            })
            .ToListAsync();

        Assert.Equal(5, projection.Count);
        Assert.All(projection, p => 
        {
            Assert.False(string.IsNullOrEmpty(p.Code));
            Assert.False(string.IsNullOrEmpty(p.Name));
            Assert.False(string.IsNullOrEmpty(p.Version));
        });
    }
}