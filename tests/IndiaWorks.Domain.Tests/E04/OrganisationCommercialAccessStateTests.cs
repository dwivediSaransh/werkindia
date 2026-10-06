using IndiaWorks.Domain.E04;
using Xunit;

namespace IndiaWorks.Domain.Tests.E04;

public sealed class OrganisationCommercialAccessStateTests
{
    [Fact]
    public void Create_WithValidParameters_SetsPropertiesCorrectly()
    {
        var orgId = Guid.NewGuid();

        var state = OrganisationCommercialAccessState.Create(orgId, 5, ResetCadence.None);

        Assert.NotEqual(Guid.Empty, state.Id);
        Assert.Equal(orgId, state.OrganisationId);
        Assert.Equal(5, state.FreeCandidateUnlockLimit);
        Assert.Equal(0, state.FreeCandidateUnlockUsed);
        Assert.Equal(AccessProvisioningState.NotProvisioned, state.ProvisioningState);
        Assert.Equal(ResetCadence.None, state.ResetCadence);
        Assert.True(state.CreatedAt <= DateTime.UtcNow);
        Assert.Null(state.UpdatedAt);
    }

    [Fact]
    public void Create_EmptyOrganisationId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            OrganisationCommercialAccessState.Create(Guid.Empty, 5, ResetCadence.None));
        Assert.Equal("organisationId", ex.ParamName);
    }

    [Fact]
    public void Create_ZeroLimit_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrganisationCommercialAccessState.Create(Guid.NewGuid(), 0, ResetCadence.None));
        Assert.Equal("freeCandidateUnlockLimit", ex.ParamName);
    }

    [Fact]
    public void Create_NegativeLimit_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrganisationCommercialAccessState.Create(Guid.NewGuid(), -1, ResetCadence.None));
        Assert.Equal("freeCandidateUnlockLimit", ex.ParamName);
    }

    [Fact]
    public void SetProvisioned_UpdatesStateAndTimestamp()
    {
        var state = OrganisationCommercialAccessState.Create(Guid.NewGuid(), 5, ResetCadence.None);
        state.SetProvisioned();

        Assert.Equal(AccessProvisioningState.Provisioned, state.ProvisioningState);
        Assert.NotNull(state.UpdatedAt);
        Assert.True(state.UpdatedAt >= state.CreatedAt);
    }

    [Fact]
    public void SetExhausted_UpdatesStateAndTimestamp()
    {
        var state = OrganisationCommercialAccessState.Create(Guid.NewGuid(), 5, ResetCadence.None);
        state.SetExhausted();

        Assert.Equal(AccessProvisioningState.Exhausted, state.ProvisioningState);
        Assert.NotNull(state.UpdatedAt);
        Assert.True(state.UpdatedAt >= state.CreatedAt);
    }

    [Fact]
    public void IncrementUsed_IncrementsUsedCount()
    {
        var state = OrganisationCommercialAccessState.Create(Guid.NewGuid(), 5, ResetCadence.None);
        state.IncrementUsed();
        state.IncrementUsed();

        Assert.Equal(2, state.FreeCandidateUnlockUsed);
        Assert.NotNull(state.UpdatedAt);
    }

    [Fact]
    public void ResetUsed_SetsUsedCountToZero()
    {
        var state = OrganisationCommercialAccessState.Create(Guid.NewGuid(), 5, ResetCadence.None);
        state.IncrementUsed();
        state.ResetUsed();

        Assert.Equal(0, state.FreeCandidateUnlockUsed);
        Assert.NotNull(state.UpdatedAt);
    }
}