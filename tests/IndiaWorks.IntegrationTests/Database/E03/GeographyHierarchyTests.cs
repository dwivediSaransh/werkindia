using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndiaWorks.IntegrationTests.Database.E03;

public sealed class GeographyHierarchyTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public GeographyHierarchyTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task CleanSqlServer_MigrationAppliesAndGeographyHierarchyPersistsAndQueries()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var india = Geography.Create(GeographyType.Country, "India", "IN");
        var karnataka = Geography.Create(GeographyType.State, "Karnataka", "KA", india.Id);
        var bengaluru = Geography.CreateWithCoordinates(
            GeographyType.City,
            "Bengaluru",
            "BLR",
            karnataka.Id,
            12.9716m,
            77.5946m);

        context.Geographies.Add(india);
        context.Geographies.Add(karnataka);
        context.Geographies.Add(bengaluru);
        await context.SaveChangesAsync();

        var retrieved = await context.Geographies
            .AsNoTracking()
            .FirstAsync(g => g.Code == "BLR");

        Assert.Equal(GeographyType.City, retrieved.Type);
        Assert.Equal("Bengaluru", retrieved.Name);
        Assert.Equal("BLR", retrieved.Code);
        Assert.Equal(karnataka.Id, retrieved.ParentId);
        Assert.Equal(12.9716m, retrieved.Latitude);
        Assert.Equal(77.5946m, retrieved.Longitude);

        var parentChain = new List<Geography>();
        Geography? current = retrieved;
        while (current is not null)
        {
            parentChain.Insert(0, current);
            current = current.ParentId is null
                ? null
                : await context.Geographies.AsNoTracking()
                    .FirstAsync(g => g.Id == current!.ParentId);
        }

        Assert.Equal(3, parentChain.Count);
        Assert.Equal("IN", parentChain[0].Code);
        Assert.Equal("KA", parentChain[1].Code);
        Assert.Equal("BLR", parentChain[2].Code);
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task CleanSqlServer_RejectsCircularParentRelationship()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var root = Geography.Create(GeographyType.Country, "Root", "RT");
        var child = Geography.Create(GeographyType.City, "Child", "CH", root.Id);

        context.Geographies.Add(root);
        context.Geographies.Add(child);
        await context.SaveChangesAsync();

        child.ReparentTo(child.Id);

        context.Geographies.Update(child);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(exception.InnerException);
    }

    [Fact(Skip = "Testcontainers SQL Server is not available. Ensure Docker is running.")]
    public async Task CleanSqlServer_EnforcesUniqueNonNullCode()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        await using var context = new AppDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var first = Geography.Create(GeographyType.Country, "First", "INDIA");
        var second = Geography.Create(GeographyType.Country, "Second", "INDIA");

        context.Geographies.Add(first);
        context.Geographies.Add(second);

        await Assert.ThrowsAnyAsync<DbUpdateException>(async () =>
        {
            await context.SaveChangesAsync();
        });
    }
}
