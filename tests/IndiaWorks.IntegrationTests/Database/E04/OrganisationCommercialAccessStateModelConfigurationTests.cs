using IndiaWorks.Domain.E04;
using IndiaWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndiaWorks.IntegrationTests.Database.E04;

public sealed class OrganisationCommercialAccessStateModelConfigurationTests
{
    private readonly AppDbContext _context;

    public OrganisationCommercialAccessStateModelConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=127.0.0.1;Database=Test_Metadata_Only;TrustServerCertificate=True;")
            .Options;

        _context = new AppDbContext(options);
    }

    [Fact]
    public void OrganisationCommercialAccessState_ModelConfiguration_MatchesSchemaRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(OrganisationCommercialAccessState));
        Assert.NotNull(entityType);

        Assert.Equal("OrganisationCommercialAccessStates", entityType.GetTableName());

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(OrganisationCommercialAccessState.Id), primaryKey.Properties.Single().Name);

        var organisationId = entityType.FindProperty(nameof(OrganisationCommercialAccessState.OrganisationId));
        Assert.NotNull(organisationId);
        Assert.False(organisationId.IsNullable);

        var freeCandidateUnlockLimit = entityType.FindProperty(nameof(OrganisationCommercialAccessState.FreeCandidateUnlockLimit));
        Assert.NotNull(freeCandidateUnlockLimit);
        Assert.False(freeCandidateUnlockLimit.IsNullable);

        var freeCandidateUnlockUsed = entityType.FindProperty(nameof(OrganisationCommercialAccessState.FreeCandidateUnlockUsed));
        Assert.NotNull(freeCandidateUnlockUsed);
        Assert.False(freeCandidateUnlockUsed.IsNullable);

        var provisioningState = entityType.FindProperty(nameof(OrganisationCommercialAccessState.ProvisioningState));
        Assert.NotNull(provisioningState);
        Assert.False(provisioningState.IsNullable);
        Assert.Equal(20, provisioningState.GetMaxLength());

        var resetCadence = entityType.FindProperty(nameof(OrganisationCommercialAccessState.ResetCadence));
        Assert.NotNull(resetCadence);
        Assert.False(resetCadence.IsNullable);
        Assert.Equal(20, resetCadence.GetMaxLength());

        var createdAt = entityType.FindProperty(nameof(OrganisationCommercialAccessState.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var updatedAt = entityType.FindProperty(nameof(OrganisationCommercialAccessState.UpdatedAt));
        Assert.NotNull(updatedAt);
        Assert.True(updatedAt.IsNullable);
    }

    [Fact]
    public void OrganisationCommercialAccessState_HasUniqueIndexOnOrganisationId()
    {
        var entityType = _context.Model.FindEntityType(typeof(OrganisationCommercialAccessState));
        Assert.NotNull(entityType);

        var uniqueIndex = entityType.GetIndexes().FirstOrDefault(i => i.IsUnique);
        Assert.NotNull(uniqueIndex);
        Assert.Equal(nameof(OrganisationCommercialAccessState.OrganisationId), uniqueIndex.Properties.Single().Name);
    }

    [Fact]
    public void OrganisationCommercialAccessState_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.OrganisationCommercialAccessStates));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<OrganisationCommercialAccessState>), dbSetProperty.PropertyType);
    }
}