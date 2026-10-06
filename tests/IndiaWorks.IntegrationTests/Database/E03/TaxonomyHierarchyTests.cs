using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndiaWorks.IntegrationTests.Database.E03;

public sealed class TaxonomyHierarchyTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public TaxonomyHierarchyTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task CleanSqlServer_MigrationAppliesAndTaxonomyPersistsAndQueries()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var version = TaxonomyVersion.Create("1.0.0", DateTime.UtcNow, TaxonomyVersionStatus.Active, "Initial");
        context.TaxonomyVersions.Add(version);
        await context.SaveChangesAsync();

        var family = JobFamily.Create("ENG", "Engineering", version.Id);
        context.JobFamilies.Add(family);
        await context.SaveChangesAsync();

        var role = Role.Create("Backend Engineer", "backend-engineer", family.Id, version.Id);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var alias = RoleAlias.Create(role.Id, "backend dev", languageCode: "en", weight: 1.0m);
        context.RoleAliases.Add(alias);

        var spec = Specialisation.Create(role.Id, "Distributed Systems", "DS");
        context.Specialisations.Add(spec);

        var workContext = WorkContext.Create(role.Id, "SHIFT_DAYTIME", "Daytime Shift");
        context.WorkContexts.Add(workContext);

        await context.SaveChangesAsync();

        var freshVersion = await context.TaxonomyVersions.AsNoTracking().FirstAsync(v => v.SemanticVersion == "1.0.0");
        Assert.Equal(TaxonomyVersionStatus.Active, freshVersion.Status);

        var freshFamily = await context.JobFamilies.AsNoTracking()
            .FirstAsync(f => f.Code == "ENG");
        Assert.Equal(freshVersion.Id, freshFamily.TaxonomyVersionId);

        var freshRole = await context.Roles.AsNoTracking()
            .FirstAsync(r => r.Slug == "backend-engineer");
        Assert.Equal(freshFamily.Id, freshRole.JobFamilyId);

        var freshAlias = await context.RoleAliases.AsNoTracking()
            .FirstAsync(a => a.Alias == "backend dev");
        Assert.Equal(freshRole.Id, freshAlias.RoleId);

        var freshSpec = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.Code == "DS");
        Assert.Equal(freshRole.Id, freshSpec.RoleId);

        var freshWorkContext = await context.WorkContexts.AsNoTracking()
            .FirstAsync(w => w.Code == "SHIFT_DAYTIME");
        Assert.Equal(freshRole.Id, freshWorkContext.RoleId);
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task CleanSqlServer_RejectsDuplicateJobFamilyCodeWithinVersion()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var version = TaxonomyVersion.Create("1.0.0", DateTime.UtcNow, TaxonomyVersionStatus.Active);
        context.TaxonomyVersions.Add(version);
        await context.SaveChangesAsync();

        var first = JobFamily.Create("ENG", "Engineering", version.Id);
        var second = JobFamily.Create("ENG", "Engineering Branch", version.Id);

        context.JobFamilies.Add(first);
        context.JobFamilies.Add(second);

        await Assert.ThrowsAnyAsync<DbUpdateException>(async () =>
        {
            await context.SaveChangesAsync();
        });
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task CleanSqlServer_RejectsDuplicateRoleSlugWithinVersion()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var version = TaxonomyVersion.Create("1.0.0", DateTime.UtcNow, TaxonomyVersionStatus.Active);
        var family = JobFamily.Create("ENG", "Engineering", version.Id);

        context.TaxonomyVersions.Add(version);
        context.JobFamilies.Add(family);
        await context.SaveChangesAsync();

        var first = Role.Create("Backend Engineer", "backend-engineer", family.Id, version.Id);
        var second = Role.Create("Backend Lead", "backend-engineer", family.Id, version.Id);

        context.Roles.Add(first);
        context.Roles.Add(second);

        await Assert.ThrowsAnyAsync<DbUpdateException>(async () =>
        {
            await context.SaveChangesAsync();
        });
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task CleanSqlServer_RejectsDuplicateRoleAliasWithinRole()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var version = TaxonomyVersion.Create("1.0.0", DateTime.UtcNow, TaxonomyVersionStatus.Active);
        var family = JobFamily.Create("ENG", "Engineering", version.Id);
        var role = Role.Create("Backend Engineer", "backend-engineer", family.Id, version.Id);

        context.TaxonomyVersions.Add(version);
        context.JobFamilies.Add(family);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var first = RoleAlias.Create(role.Id, "backend dev");
        var second = RoleAlias.Create(role.Id, "backend dev");

        context.RoleAliases.Add(first);
        context.RoleAliases.Add(second);

        await Assert.ThrowsAnyAsync<DbUpdateException>(async () =>
        {
            await context.SaveChangesAsync();
        });
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task CleanSqlServer_CascadeDeletesRoleAliasesWhenRoleIsDeleted()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var version = TaxonomyVersion.Create("1.0.0", DateTime.UtcNow, TaxonomyVersionStatus.Active);
        var family = JobFamily.Create("ENG", "Engineering", version.Id);
        var role = Role.Create("Backend Engineer", "backend-engineer", family.Id, version.Id);
        var alias = RoleAlias.Create(role.Id, "backend dev");
        var spec = Specialisation.Create(role.Id, "Distributed Systems", "DS");
        var workContext = WorkContext.Create(role.Id, "SHIFT_DAYTIME", "Daytime Shift");

        context.TaxonomyVersions.Add(version);
        context.JobFamilies.Add(family);
        context.Roles.Add(role);
        context.RoleAliases.Add(alias);
        context.Specialisations.Add(spec);
        context.WorkContexts.Add(workContext);
        await context.SaveChangesAsync();

        var roleId = role.Id;
        context.Roles.Remove(role);
        await context.SaveChangesAsync();

        var missingRole = await context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId);
        Assert.Null(missingRole);

        var stillHasAlias = await context.RoleAliases.AsNoTracking().AnyAsync(a => a.RoleId == roleId);
        Assert.False(stillHasAlias);

        var stillHasSpec = await context.Specialisations.AsNoTracking().AnyAsync(s => s.RoleId == roleId);
        Assert.False(stillHasSpec);

        var stillHasWorkContext = await context.WorkContexts.AsNoTracking().AnyAsync(w => w.RoleId == roleId);
        Assert.False(stillHasWorkContext);
    }
}
