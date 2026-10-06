using IndiaWorks.Domain.E02;
using Xunit;

namespace IndiaWorks.Domain.Tests.E02;

public sealed class OrganisationMembershipTests
{
    [Fact]
    public void Create_ValidParameters_SetsPropertiesCorrectly()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var membership = OrganisationMembership.Create(orgId, userId, OrganisationRole.Admin);

        Assert.Equal(orgId, membership.OrganisationId);
        Assert.Equal(userId, membership.UserAccountId);
        Assert.Equal(OrganisationRole.Admin, membership.Role);
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.True(membership.CreatedAt <= DateTime.UtcNow);
        Assert.Null(membership.UpdatedAt);
    }

    [Fact]
    public void Create_EmptyOrganisationId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            OrganisationMembership.Create(Guid.Empty, Guid.NewGuid(), OrganisationRole.Owner));
    }

    [Fact]
    public void Create_EmptyUserAccountId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            OrganisationMembership.Create(Guid.NewGuid(), Guid.Empty, OrganisationRole.Owner));
    }

    [Fact]
    public void ChangeRole_WhenActive_UpdatesRoleAndTimestamp()
    {
        var membership = OrganisationMembership.Create(Guid.NewGuid(), Guid.NewGuid(), OrganisationRole.Viewer);
        membership.ChangeRole(OrganisationRole.Recruiter);

        Assert.Equal(OrganisationRole.Recruiter, membership.Role);
        Assert.NotNull(membership.UpdatedAt);
    }

    [Fact]
    public void ChangeRole_WhenSuspended_ThrowsInvalidOperationException()
    {
        var membership = OrganisationMembership.Create(Guid.NewGuid(), Guid.NewGuid(), OrganisationRole.Viewer);
        membership.Suspend();

        Assert.Throws<InvalidOperationException>(() =>
            membership.ChangeRole(OrganisationRole.Recruiter));
    }

    [Fact]
    public void Suspend_And_Reactivate_Cycle_Works()
    {
        var membership = OrganisationMembership.Create(Guid.NewGuid(), Guid.NewGuid(), OrganisationRole.Recruiter);
        membership.Suspend();
        Assert.Equal(MembershipStatus.Suspended, membership.Status);

        membership.Reactivate();
        Assert.Equal(MembershipStatus.Active, membership.Status);
    }

    [Fact]
    public void Terminate_Prevents_Reactivation()
    {
        var membership = OrganisationMembership.Create(Guid.NewGuid(), Guid.NewGuid(), OrganisationRole.Recruiter);
        membership.Terminate();
        Assert.Equal(MembershipStatus.Terminated, membership.Status);

        Assert.Throws<InvalidOperationException>(() => membership.Reactivate());
        Assert.Throws<InvalidOperationException>(() => membership.Suspend());
    }

    [Theory]
    [InlineData(OrganisationRole.Owner, true, true, true)]
    [InlineData(OrganisationRole.Admin, true, false, true)]
    [InlineData(OrganisationRole.Recruiter, false, false, true)]
    [InlineData(OrganisationRole.Viewer, false, false, false)]
    [InlineData(OrganisationRole.BillingAdmin, false, true, false)]
    public void RolePermissions_MatchExpected(OrganisationRole role, bool canManageTeam, bool canManageBilling, bool canManageJobs)
    {
        var membership = OrganisationMembership.Create(Guid.NewGuid(), Guid.NewGuid(), role);

        Assert.Equal(canManageTeam, membership.CanManageTeam());
        Assert.Equal(canManageBilling, membership.CanManageBilling());
        Assert.Equal(canManageJobs, membership.CanManageJobs());
    }
}
