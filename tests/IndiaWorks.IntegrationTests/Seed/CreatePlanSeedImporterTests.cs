using System.IO;
using IndiaWorks.Domain.E04;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;
using IndiaWorks.IntegrationTests.Database;

namespace IndiaWorks.IntegrationTests.Seed;

public sealed class CreatePlanSeedImporterTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public CreatePlanSeedImporterTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task FreshDatabase_SeedRunner_PersistsPlansVersionsAndEntitlements()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var seedJson = await ReadSeedJsonAsync();

        await CreatePlanSeedImporter.SeedAsync(context, seedJson);

        var planCount = await context.SubscriptionPlans.CountAsync();
        Assert.Equal(3, planCount);

        var versionCount = await context.SubscriptionPlanVersions.CountAsync();
        Assert.Equal(6, versionCount);

        var entitlementCount = await context.EntitlementDefinitions.CountAsync();
        Assert.Equal(3, entitlementCount);

        var planEntitlementCount = await context.PlanEntitlements.CountAsync();
        Assert.Equal(18, planEntitlementCount);

        var freePlan = await context.SubscriptionPlans.AsNoTracking()
            .FirstAsync(p => p.Code == "FREE_EVALUATION");
        Assert.True(freePlan.IsActive);

        var proPlan = await context.SubscriptionPlans.AsNoTracking()
            .FirstAsync(p => p.Code == "PRO");

        var starterMonthly = await context.SubscriptionPlanVersions.AsNoTracking()
            .FirstAsync(v => v.PlanId == freePlan.Id && v.Interval == BillingInterval.Monthly);
        Assert.Equal("INR", starterMonthly.Currency);
        Assert.Null(starterMonthly.Amount);

        var proYearly = await context.SubscriptionPlanVersions.AsNoTracking()
            .FirstAsync(v => v.PlanId == proPlan.Id && v.Interval == BillingInterval.Yearly);
        Assert.Equal("INR", proYearly.Currency);
        Assert.Null(proYearly.Amount);

        var freeVersion = await context.SubscriptionPlanVersions.AsNoTracking()
            .FirstAsync(v => v.PlanId == freePlan.Id && v.Interval == BillingInterval.Monthly);
        var freeCandidateViews = await context.PlanEntitlements.AsNoTracking()
            .FirstAsync(pe => pe.PlanVersionId == freeVersion.Id
                && pe.EntitlementDefinitionCode == "CANDIDATE_VIEWS");
        Assert.True(freeCandidateViews.Enabled);
        Assert.Equal(5, freeCandidateViews.Limit);

        var freeContactReveals = await context.PlanEntitlements.AsNoTracking()
            .FirstAsync(pe => pe.PlanVersionId == freeVersion.Id
                && pe.EntitlementDefinitionCode == "CONTACT_REVEALS");
        Assert.False(freeContactReveals.Enabled);
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task FreshDatabase_SeedRunner_IsIdempotent_WhenRunTwice()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var seedJson = await ReadSeedJsonAsync();

        await CreatePlanSeedImporter.SeedAsync(context, seedJson);
        var firstPlanCount = await context.SubscriptionPlans.CountAsync();
        var firstVersionCount = await context.SubscriptionPlanVersions.CountAsync();

        await CreatePlanSeedImporter.SeedAsync(context, seedJson);
        var secondPlanCount = await context.SubscriptionPlans.CountAsync();
        var secondVersionCount = await context.SubscriptionPlanVersions.CountAsync();

        Assert.Equal(firstPlanCount, secondPlanCount);
        Assert.Equal(firstVersionCount, secondVersionCount);
    }

    [Fact]
    public async Task SeedRunner_RejectsUnknownBillingInterval()
    {
        var seedJson = """
        {
          "version": "1.0.0",
          "semanticVersion": "1.0.0",
          "status": "Active",
          "effectiveFrom": "2026-01-01T00:00:00Z",
          "rows": [
            {"entityType": "SubscriptionPlan", "code": "TEST", "name": "Test Plan"},
            {"entityType": "PlanVersion", "planCode": "TEST", "interval": "UnknownInterval", "currency": "INR"}
          ]
        }
        """;

        await using var context = new AppDbContext(CreateInMemoryOptions());
        await Assert.ThrowsAsync<SeedDataException>(() =>
            CreatePlanSeedImporter.SeedAsync(context, seedJson));
    }

    [Fact]
    public async Task SeedRunner_RejectsUnknownPlanCodeInPlanVersion()
    {
        var seedJson = """
        {
          "version": "1.0.0",
          "semanticVersion": "1.0.0",
          "status": "Active",
          "effectiveFrom": "2026-01-01T00:00:00Z",
          "rows": [
            {"entityType": "PlanVersion", "planCode": "NONEXISTENT", "interval": "Monthly", "currency": "INR"}
          ]
        }
        """;

        await using var context = new AppDbContext(CreateInMemoryOptions());
        await Assert.ThrowsAsync<SeedDataException>(() =>
            CreatePlanSeedImporter.SeedAsync(context, seedJson));
    }

    [Fact]
    public void SeedDataset_ContainsNoForbiddenSecretOrContactData()
    {
        var seedJson = File.ReadAllText(System.IO.Path.Combine(GetRepoRoot(), "src", "backend",
            "IndiaWorks.Infrastructure", "Persistence", "Seed",
            "seed-free-evaluation-and-paid-plan-skeletons.json"));

        var lower = seedJson.ToLowerInvariant();
        Assert.DoesNotContain("+91", lower);
        Assert.DoesNotContain("@", lower);
        Assert.DoesNotContain("otp", lower);
        Assert.DoesNotContain("token", lower);
        Assert.DoesNotContain("aadh", lower);
        Assert.DoesNotContain("pan ", lower);
    }

    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nameof(CreatePlanSeedImporterTests))
            .Options;
    }

    private static async Task<string> ReadSeedJsonAsync()
    {
        var repoRoot = GetRepoRoot();
        var path = System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "seed-free-evaluation-and-paid-plan-skeletons.json");
        return await File.ReadAllTextAsync(path);
    }

    private static string GetRepoRoot()
    {
        var assemblyLocation = typeof(CreatePlanSeedImporterTests).Assembly.Location;
        var testProjectDir = System.IO.Path.GetDirectoryName(assemblyLocation)!;
        var candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
        return candidate;
    }
}