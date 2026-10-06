using IndiaWorks.Domain.E04;
using Xunit;

namespace IndiaWorks.Domain.Tests.E04;

public sealed class EntitlementUsageEventTests
{
    [Fact]
    public void Create_WithValidParameters_SetsPropertiesCorrectly()
    {
        var orgId = Guid.NewGuid();
        var planVersionId = Guid.NewGuid();
        var idempotencyKey = "evt-abc-123";

        var usageEvent = EntitlementUsageEvent.Create(orgId, "candidate_views", planVersionId, 5m, idempotencyKey);

        Assert.NotEqual(Guid.Empty, usageEvent.Id);
        Assert.Equal(orgId, usageEvent.OrganisationId);
        Assert.Equal("candidate_views", usageEvent.EntitlementDefinitionCode);
        Assert.Equal(planVersionId, usageEvent.PlanVersionId);
        Assert.Equal(5m, usageEvent.Quantity);
        Assert.True(usageEvent.EventDate <= DateTime.UtcNow);
        Assert.Equal(idempotencyKey, usageEvent.IdempotencyKey);
        Assert.True(usageEvent.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_EmptyOrganisationId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            EntitlementUsageEvent.Create(Guid.Empty, "candidate_views", Guid.NewGuid(), 5m, "key-1"));
        Assert.Equal("organisationId", ex.ParamName);
    }

    [Fact]
    public void Create_NullEntitlementDefinitionCode_ThrowsArgumentException()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            EntitlementUsageEvent.Create(Guid.NewGuid(), null!, Guid.NewGuid(), 5m, "key-1"));
        Assert.Equal("entitlementDefinitionCode", ex.ParamName);
    }

    [Fact]
    public void Create_EmptyIdempotencyKey_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            EntitlementUsageEvent.Create(Guid.NewGuid(), "candidate_views", Guid.NewGuid(), 5m, ""));
        Assert.Equal("idempotencyKey", ex.ParamName);
    }

    [Fact]
    public void Create_TrimsEntitlementDefinitionCode()
    {
        var usageEvent = EntitlementUsageEvent.Create(Guid.NewGuid(), "  candidate_views  ", Guid.NewGuid(), 5m, "key-1");
        Assert.Equal("candidate_views", usageEvent.EntitlementDefinitionCode);
    }

    [Fact]
    public void Create_TrimsIdempotencyKey()
    {
        var usageEvent = EntitlementUsageEvent.Create(Guid.NewGuid(), "candidate_views", Guid.NewGuid(), 5m, "  key-1  ");
        Assert.Equal("key-1", usageEvent.IdempotencyKey);
    }
}