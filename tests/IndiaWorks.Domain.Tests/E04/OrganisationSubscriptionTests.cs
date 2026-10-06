using IndiaWorks.Domain.E04;
using Xunit;

namespace IndiaWorks.Domain.Tests.E04;

public sealed class OrganisationSubscriptionTests
{
    [Fact]
    public void Create_WithValidParameters_SetsPropertiesCorrectly()
    {
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var periodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var subscription = OrganisationSubscription.Create(orgId, planId, periodStart, periodEnd);

        Assert.NotEqual(Guid.Empty, subscription.Id);
        Assert.Equal(orgId, subscription.OrganisationId);
        Assert.Equal(planId, subscription.PlanId);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.Equal(periodStart, subscription.CurrentPeriodStart);
        Assert.Equal(periodEnd, subscription.CurrentPeriodEnd);
        Assert.False(subscription.CancelAtPeriodEnd);
        Assert.True(subscription.CreatedAt <= DateTime.UtcNow);
        Assert.Null(subscription.UpdatedAt);
    }

    [Fact]
    public void Create_EmptyOrganisationId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            OrganisationSubscription.Create(Guid.Empty, Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(30)));
        Assert.Equal("organisationId", ex.ParamName);
    }

    [Fact]
    public void Create_EmptyPlanId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            OrganisationSubscription.Create(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, DateTime.UtcNow.AddDays(30)));
        Assert.Equal("planId", ex.ParamName);
    }

    [Fact]
    public void UpdateStatus_UpdatesStatusAndTimestamp()
    {
        var subscription = OrganisationSubscription.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        subscription.UpdateStatus(SubscriptionStatus.Active);

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.NotNull(subscription.UpdatedAt);
        Assert.True(subscription.UpdatedAt >= subscription.CreatedAt);
    }

    [Fact]
    public void SetCancelAtPeriodEnd_SetsFlag()
    {
        var subscription = OrganisationSubscription.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        subscription.SetCancelAtPeriodEnd(true);

        Assert.True(subscription.CancelAtPeriodEnd);
        Assert.NotNull(subscription.UpdatedAt);
    }
}