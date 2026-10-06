using IndiaWorks.Domain.E02;
using Xunit;

namespace IndiaWorks.Domain.Tests.E02;

public sealed class OrganisationInvitationTests
{
    private static readonly Guid ValidOrgId = Guid.NewGuid();
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private static readonly byte[] ValidHash = new byte[32];
    private static readonly byte[] ValidTokenHash = new byte[64];

    static OrganisationInvitationTests()
    {
        Random.Shared.NextBytes(ValidHash);
        Random.Shared.NextBytes(ValidTokenHash);
    }

    [Fact]
    public void Create_with_valid_inputs_returns_pending_invitation()
    {
        var expiry = DateTime.UtcNow.AddDays(7);
        var invite = OrganisationInvitation.Create(
            ValidOrgId, ValidUserId, "Email", ValidHash,
            OrganisationRole.Recruiter, ValidTokenHash, expiry);

        Assert.NotEqual(Guid.Empty, invite.Id);
        Assert.Equal(ValidOrgId, invite.OrganisationId);
        Assert.Equal(ValidUserId, invite.InvitedByUserAccountId);
        Assert.Equal("Email", invite.InviteeIdentifierType);
        Assert.Equal(ValidHash, invite.InviteeIdentifierHash);
        Assert.Equal(OrganisationRole.Recruiter, invite.Role);
        Assert.Equal(ValidTokenHash, invite.TokenHash);
        Assert.Equal(InvitationStatus.Pending, invite.Status);
        Assert.Equal(expiry, invite.ExpiresAt);
        Assert.True((DateTime.UtcNow - invite.CreatedAt).TotalSeconds < 5);
        Assert.Null(invite.AcceptedByUserAccountId);
        Assert.Null(invite.AcceptedAt);
        Assert.Null(invite.RevokedAt);
        Assert.True(invite.IsActive);
    }

    [Fact]
    public void Create_with_empty_organisationId_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            OrganisationInvitation.Create(Guid.Empty, ValidUserId, "Email",
                ValidHash, OrganisationRole.Recruiter, ValidTokenHash, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_with_empty_invitedByUserAccountId_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            OrganisationInvitation.Create(ValidOrgId, Guid.Empty, "Email",
                ValidHash, OrganisationRole.Recruiter, ValidTokenHash, DateTime.UtcNow.AddDays(1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_with_blank_identifierType_throws(string identifierType)
    {
        Assert.Throws<ArgumentException>(() =>
            OrganisationInvitation.Create(ValidOrgId, ValidUserId, identifierType,
                ValidHash, OrganisationRole.Recruiter, ValidTokenHash, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_with_null_identifierType_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            OrganisationInvitation.Create(ValidOrgId, ValidUserId, null!,
                ValidHash, OrganisationRole.Recruiter, ValidTokenHash, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_with_past_expiry_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            OrganisationInvitation.Create(ValidOrgId, ValidUserId, "Email",
                ValidHash, OrganisationRole.Recruiter, ValidTokenHash, DateTime.UtcNow.AddSeconds(-1)));
    }

    [Fact]
    public void Accept_transitions_status_to_Accepted()
    {
        var invite = OrganisationInvitation.Create(
            ValidOrgId, ValidUserId, "Email", ValidHash,
            OrganisationRole.Recruiter, ValidTokenHash, DateTime.UtcNow.AddDays(7));
        var acceptorId = Guid.NewGuid();

        invite.Accept(acceptorId);

        Assert.Equal(InvitationStatus.Accepted, invite.Status);
        Assert.Equal(acceptorId, invite.AcceptedByUserAccountId);
        Assert.NotNull(invite.AcceptedAt);
        Assert.False(invite.IsActive);
    }

    [Fact]
    public void Accept_already_accepted_invitation_throws()
    {
        var invite = OrganisationInvitation.Create(
            ValidOrgId, ValidUserId, "Email", ValidHash,
            OrganisationRole.Recruiter, ValidTokenHash, DateTime.UtcNow.AddDays(7));
        invite.Accept(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => invite.Accept(Guid.NewGuid()));
    }

    [Fact]
    public void Revoke_transitions_status_to_Revoked()
    {
        var invite = OrganisationInvitation.Create(
            ValidOrgId, ValidUserId, "Email", ValidHash,
            OrganisationRole.Recruiter, ValidTokenHash, DateTime.UtcNow.AddDays(7));

        invite.Revoke();

        Assert.Equal(InvitationStatus.Revoked, invite.Status);
        Assert.NotNull(invite.RevokedAt);
        Assert.False(invite.IsActive);
    }

    [Fact]
    public void Revoke_already_revoked_invitation_throws()
    {
        var invite = OrganisationInvitation.Create(
            ValidOrgId, ValidUserId, "Email", ValidHash,
            OrganisationRole.Recruiter, ValidTokenHash, DateTime.UtcNow.AddDays(7));
        invite.Revoke();

        Assert.Throws<InvalidOperationException>(() => invite.Revoke());
    }

    [Fact]
    public void IsActive_returns_false_for_revoked_invitation()
    {
        var invite = OrganisationInvitation.Create(
            ValidOrgId, ValidUserId, "Email", ValidHash,
            OrganisationRole.Recruiter, ValidTokenHash, DateTime.UtcNow.AddDays(7));
        invite.Revoke();

        Assert.False(invite.IsActive);
    }
}
