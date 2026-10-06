using IndiaWorks.Domain.E02;
using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndiaWorks.IntegrationTests.Database.E03;

public sealed class GeographyModelConfigurationTests
{
    private readonly AppDbContext _context;

    public GeographyModelConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=127.0.0.1;Database=Test_Metadata_Only;TrustServerCertificate=True;")
            .Options;

        _context = new AppDbContext(options);
    }

    [Fact]
    public void Geography_ModelConfiguration_MatchesHierarchyRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(Geography));
        Assert.NotNull(entityType);

        Assert.Equal("Geographies", entityType.GetTableName());

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(Geography.Id), primaryKey.Properties.Single().Name);

        var name = entityType.FindProperty(nameof(Geography.Name));
        Assert.NotNull(name);
        Assert.False(name.IsNullable);
        Assert.Equal(200, name.GetMaxLength());

        var code = entityType.FindProperty(nameof(Geography.Code));
        Assert.NotNull(code);
        Assert.True(code.IsNullable);
        Assert.Equal(30, code.GetMaxLength());

        var type = entityType.FindProperty(nameof(Geography.Type));
        Assert.NotNull(type);
        Assert.False(type.IsNullable);
        Assert.Equal(20, type.GetMaxLength());

        var parentId = entityType.FindProperty(nameof(Geography.ParentId));
        Assert.NotNull(parentId);
        Assert.True(parentId.IsNullable);

        var latitude = entityType.FindProperty(nameof(Geography.Latitude));
        Assert.NotNull(latitude);
        Assert.True(latitude.IsNullable);
        Assert.Equal(9, latitude.GetPrecision());
        Assert.Equal(6, latitude.GetScale());

        var createdAt = entityType.FindProperty(nameof(Geography.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var parentNavigation = entityType.FindNavigation(nameof(Geography.Parent));
        Assert.NotNull(parentNavigation);
        Assert.False(parentNavigation.IsCollection);

        var childrenNavigation = entityType.FindNavigation(nameof(Geography.Children));
        Assert.NotNull(childrenNavigation);
        Assert.True(childrenNavigation.IsCollection);

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var parentFk = foreignKeys.First(fk => fk.PrincipalEntityType.ClrType == typeof(Geography));
        Assert.Equal(DeleteBehavior.Restrict, parentFk.DeleteBehavior);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Geography.Code)) && i.IsUnique);
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Geography.ParentId)));
        Assert.Contains(indexes, i => i.Properties.Count == 2
            && i.Properties.Any(p => p.Name == nameof(Geography.Type))
            && i.Properties.Any(p => p.Name == nameof(Geography.Name)));
    }

    [Fact]
    public void Geographies_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.Geographies));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<Geography>), dbSetProperty.PropertyType);
    }
}
