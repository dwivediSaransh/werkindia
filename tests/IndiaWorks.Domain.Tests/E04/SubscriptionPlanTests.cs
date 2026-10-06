using IndiaWorks.Domain.E04;
using Xunit;

namespace IndiaWorks.Domain.Tests.E04;

public sealed class SubscriptionPlanTests
{
    [Fact]
    public void Create_WithValidParameters_SetsPropertiesCorrectly()
    {
        var plan = SubscriptionPlan.Create("PRO", "Professional Plan", "Best plan");

        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal("PRO", plan.Code);
        Assert.Equal("Professional Plan", plan.Name);
        Assert.Equal("Best plan", plan.Description);
        Assert.True(plan.IsActive);
        Assert.True(plan.CreatedAt <= DateTime.UtcNow);
        Assert.Empty(plan.Versions);
    }

    [Fact]
    public void Create_WithNullDescription_SetsDescriptionToNull()
    {
        var plan = SubscriptionPlan.Create("PRO", "Professional Plan");

        Assert.Null(plan.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankCode_ThrowsArgumentException(string? code)
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            SubscriptionPlan.Create(code!, "Professional Plan"));
        Assert.Equal("code", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsArgumentException(string? name)
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            SubscriptionPlan.Create("PRO", name!));
        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var plan = SubscriptionPlan.Create("PRO", "Professional Plan");
        plan.Deactivate();

        Assert.False(plan.IsActive);
    }

    [Fact]
    public void Reactivate_SetsIsActiveToTrue()
    {
        var plan = SubscriptionPlan.Create("PRO", "Professional Plan");
        plan.Deactivate();
        plan.Reactivate();

        Assert.True(plan.IsActive);
    }

    [Fact]
    public void AddVersion_AddsVersionToCollection()
    {
        var plan = SubscriptionPlan.Create("PRO", "Professional Plan");
        var version = plan.AddVersion(BillingInterval.Monthly, "INR", 999m);

        Assert.Single(plan.Versions);
        Assert.Equal(version.Id, plan.Versions.First().Id);
        Assert.Equal(BillingInterval.Monthly, plan.Versions.First().Interval);
        Assert.Equal("INR", plan.Versions.First().Currency);
        Assert.Equal(999m, plan.Versions.First().Amount);
    }
}