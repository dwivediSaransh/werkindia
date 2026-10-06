using IndiaWorks.Domain.E04;
using IndiaWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndiaWorks.IntegrationTests.Database.E04;

public sealed class CandidatePaywallEventModelConfigurationTests
{
    private readonly AppDbContext _context;

    public CandidatePaywallEventModelConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=127.0.0.1;Database=Test_Metadata_Only;TrustServerCertificate=True;")
            .Options;

        _context = new AppDbContext(options);
    }

    [Fact]
    public void CandidatePaywallEvent_ModelConfiguration_MatchesSchemaRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(CandidatePaywallEvent));
        Assert.NotNull(entityType);

        Assert.Equal("CandidatePaywallEvents", entityType.GetTableName());
        Assert.Equal(nameof(CandidatePaywallEvent.Id), entityType.FindPrimaryKey()?.Properties.Single().Name);

        AssertRequired(nameof(CandidatePaywallEvent.OrganisationId));
        AssertRequired(nameof(CandidatePaywallEvent.CandidateProfileId));
        AssertRequired(nameof(CandidatePaywallEvent.FreeLimit));
        AssertRequired(nameof(CandidatePaywallEvent.FreeUsed));
        AssertRequired(nameof(CandidatePaywallEvent.FreeRemaining));
        AssertRequired(nameof(CandidatePaywallEvent.AccessSource));
        AssertRequired(nameof(CandidatePaywallEvent.CorrelationId));
        AssertRequired(nameof(CandidatePaywallEvent.CreatedAt));

        Assert.Equal(500, entityType.FindProperty(nameof(CandidatePaywallEvent.Reason))?.GetMaxLength());
        Assert.Equal(250, entityType.FindProperty(nameof(CandidatePaywallEvent.JobContext))?.GetMaxLength());
        Assert.Equal(20, entityType.FindProperty(nameof(CandidatePaywallEvent.AccessSource))?.GetMaxLength());

        void AssertRequired(string propertyName)
        {
            var property = entityType.FindProperty(propertyName);
            Assert.NotNull(property);
            Assert.False(property.IsNullable);
        }
    }

    [Fact]
    public void CandidatePaywallEvent_HasExpectedIndexes()
    {
        var entityType = _context.Model.FindEntityType(typeof(CandidatePaywallEvent));
        Assert.NotNull(entityType);

        var correlationIdIndex = entityType.GetIndexes()
            .SingleOrDefault(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(CandidatePaywallEvent.CorrelationId) }));
        Assert.NotNull(correlationIdIndex);
        Assert.True(correlationIdIndex.IsUnique);

        var lookupIndex = entityType.GetIndexes()
            .SingleOrDefault(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[]
                {
                    nameof(CandidatePaywallEvent.OrganisationId),
                    nameof(CandidatePaywallEvent.CandidateProfileId),
                    nameof(CandidatePaywallEvent.CreatedAt)
                }));
        Assert.NotNull(lookupIndex);
    }

    [Fact]
    public void CandidatePaywallEvent_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.CandidatePaywallEvents));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<CandidatePaywallEvent>), dbSetProperty.PropertyType);
    }
}
