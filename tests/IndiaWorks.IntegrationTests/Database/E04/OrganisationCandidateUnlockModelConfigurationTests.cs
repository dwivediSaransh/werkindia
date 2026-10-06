using IndiaWorks.Domain.E04;
using IndiaWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndiaWorks.IntegrationTests.Database.E04;

public sealed class OrganisationCandidateUnlockModelConfigurationTests
{
    private readonly AppDbContext _context;

    public OrganisationCandidateUnlockModelConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=127.0.0.1;Database=Test_Metadata_Only;TrustServerCertificate=True;")
            .Options;

        _context = new AppDbContext(options);
    }

    [Fact]
    public void OrganisationCandidateUnlock_ModelConfiguration_MatchesSchemaRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(OrganisationCandidateUnlock));
        Assert.NotNull(entityType);

        Assert.Equal("OrganisationCandidateUnlocks", entityType.GetTableName());

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(OrganisationCandidateUnlock.Id), primaryKey.Properties.Single().Name);

        var organisationId = entityType.FindProperty(nameof(OrganisationCandidateUnlock.OrganisationId));
        Assert.NotNull(organisationId);
        Assert.False(organisationId.IsNullable);

        var candidateProfileId = entityType.FindProperty(nameof(OrganisationCandidateUnlock.CandidateProfileId));
        Assert.NotNull(candidateProfileId);
        Assert.False(candidateProfileId.IsNullable);

        var source = entityType.FindProperty(nameof(OrganisationCandidateUnlock.Source));
        Assert.NotNull(source);
        Assert.False(source.IsNullable);
        Assert.Equal(30, source.GetMaxLength());

        var unlockedAt = entityType.FindProperty(nameof(OrganisationCandidateUnlock.UnlockedAt));
        Assert.NotNull(unlockedAt);
        Assert.False(unlockedAt.IsNullable);
    }

    [Fact]
    public void OrganisationCandidateUnlock_HasUniqueCompositeIndexOnOrganisationId_CandidateProfileId()
    {
        var entityType = _context.Model.FindEntityType(typeof(OrganisationCandidateUnlock));
        Assert.NotNull(entityType);

        var uniqueIndex = entityType.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Count == 2);
        Assert.NotNull(uniqueIndex);
        var props = uniqueIndex.Properties.Select(p => p.Name).ToList();
        Assert.Contains(nameof(OrganisationCandidateUnlock.OrganisationId), props);
        Assert.Contains(nameof(OrganisationCandidateUnlock.CandidateProfileId), props);
    }

    [Fact]
    public void OrganisationCandidateUnlock_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.OrganisationCandidateUnlocks));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<OrganisationCandidateUnlock>), dbSetProperty.PropertyType);
    }
}