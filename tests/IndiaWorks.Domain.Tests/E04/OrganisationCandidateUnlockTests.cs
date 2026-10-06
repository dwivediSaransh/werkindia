using IndiaWorks.Domain.E04;
using Xunit;

namespace IndiaWorks.Domain.Tests.E04;

public sealed class OrganisationCandidateUnlockTests
{
    [Fact]
    public void Create_WithValidParameters_SetsPropertiesCorrectly()
    {
        var orgId = Guid.NewGuid();
        var candidateProfileId = Guid.NewGuid();

        var unlock = OrganisationCandidateUnlock.Create(orgId, candidateProfileId, UnlockSource.FreeAllowance);

        Assert.NotEqual(Guid.Empty, unlock.Id);
        Assert.Equal(orgId, unlock.OrganisationId);
        Assert.Equal(candidateProfileId, unlock.CandidateProfileId);
        Assert.Equal(UnlockSource.FreeAllowance, unlock.Source);
        Assert.True(unlock.UnlockedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_EmptyOrganisationId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            OrganisationCandidateUnlock.Create(Guid.Empty, Guid.NewGuid(), UnlockSource.FreeAllowance));
        Assert.Equal("organisationId", ex.ParamName);
    }

    [Fact]
    public void Create_EmptyCandidateProfileId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            OrganisationCandidateUnlock.Create(Guid.NewGuid(), Guid.Empty, UnlockSource.PaidEntitlement));
        Assert.Equal("candidateProfileId", ex.ParamName);
    }

    [Fact]
    public void Create_FreeAllowance_SourceIsFreeAllowance()
    {
        var unlock = OrganisationCandidateUnlock.Create(Guid.NewGuid(), Guid.NewGuid(), UnlockSource.FreeAllowance);
        Assert.Equal(UnlockSource.FreeAllowance, unlock.Source);
    }

    [Fact]
    public void Create_PaidEntitlement_SourceIsPaidEntitlement()
    {
        var unlock = OrganisationCandidateUnlock.Create(Guid.NewGuid(), Guid.NewGuid(), UnlockSource.PaidEntitlement);
        Assert.Equal(UnlockSource.PaidEntitlement, unlock.Source);
    }

    [Fact]
    public void Create_AddOn_SourceIsAddOn()
    {
        var unlock = OrganisationCandidateUnlock.Create(Guid.NewGuid(), Guid.NewGuid(), UnlockSource.AddOn);
        Assert.Equal(UnlockSource.AddOn, unlock.Source);
    }

    [Fact]
    public void Create_Admin_SourceIsAdmin()
    {
        var unlock = OrganisationCandidateUnlock.Create(Guid.NewGuid(), Guid.NewGuid(), UnlockSource.Admin);
        Assert.Equal(UnlockSource.Admin, unlock.Source);
    }
}