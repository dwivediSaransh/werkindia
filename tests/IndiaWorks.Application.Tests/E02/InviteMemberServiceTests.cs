using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Domain.E02;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndiaWorks.Application.Tests.E02;

public sealed class InviteMemberServiceTests
{
    private readonly InMemoryOrgRepository _orgRepo = new();
    private readonly InMemoryMembershipRepository _membershipRepo = new();
    private readonly InMemoryInvitationRepository _inviteRepo = new();
    private readonly OrganisationMemberInviteService _service;

    private static readonly byte[] SampleHash = new byte[32];
    private static readonly byte[] SampleTokenHash = new byte[64];

    static InviteMemberServiceTests()
    {
        Random.Shared.NextBytes(SampleHash);
        Random.Shared.NextBytes(SampleTokenHash);
    }

    public InviteMemberServiceTests()
    {
        _service = new OrganisationMemberInviteService(
            _orgRepo, _membershipRepo, _inviteRepo,
            NullLogger<OrganisationMemberInviteService>.Instance);
    }

    // ── Invite ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task InviteAsync_HappyPath_Owner_CreatesInvitation()
    {
        var orgId = SeedOrg();
        var ownerId = SeedMember(orgId, OrganisationRole.Owner);
        var expiry = DateTime.UtcNow.AddDays(7);

        var result = await _service.InviteAsync(new InviteMemberRequest(
            OrganisationId: orgId,
            InvitedByUserAccountId: ownerId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: SampleHash,
            Role: OrganisationRole.Recruiter,
            TokenHash: SampleTokenHash,
            ExpiresAt: expiry));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.InvitationId);
        Assert.NotEqual(Guid.Empty, result.InvitationId.Value);
    }

    [Fact]
    public async Task InviteAsync_Admin_CanInvite()
    {
        var orgId = SeedOrg();
        var adminId = SeedMember(orgId, OrganisationRole.Admin);

        var result = await _service.InviteAsync(new InviteMemberRequest(
            orgId, adminId, "Email", SampleHash,
            OrganisationRole.Viewer, SampleTokenHash, DateTime.UtcNow.AddDays(3)));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task InviteAsync_Recruiter_Returns_RoleNotAllowed()
    {
        var orgId = SeedOrg();
        var recruiterId = SeedMember(orgId, OrganisationRole.Recruiter);

        var result = await _service.InviteAsync(new InviteMemberRequest(
            orgId, recruiterId, "Email", SampleHash,
            OrganisationRole.Viewer, SampleTokenHash, DateTime.UtcNow.AddDays(3)));

        Assert.False(result.IsSuccess);
        Assert.Equal("ROLE_NOT_ALLOWED", result.ErrorCode);
    }

    [Fact]
    public async Task InviteAsync_NonMember_Returns_MembershipRequired()
    {
        var orgId = SeedOrg();
        var strangerUserId = Guid.NewGuid();

        var result = await _service.InviteAsync(new InviteMemberRequest(
            orgId, strangerUserId, "Email", SampleHash,
            OrganisationRole.Viewer, SampleTokenHash, DateTime.UtcNow.AddDays(3)));

        Assert.False(result.IsSuccess);
        Assert.Equal("MEMBERSHIP_REQUIRED", result.ErrorCode);
    }

    [Fact]
    public async Task InviteAsync_OrgNotFound_Returns_OrgNotFound()
    {
        var result = await _service.InviteAsync(new InviteMemberRequest(
            Guid.NewGuid(), Guid.NewGuid(), "Email", SampleHash,
            OrganisationRole.Viewer, SampleTokenHash, DateTime.UtcNow.AddDays(3)));

        Assert.False(result.IsSuccess);
        Assert.Equal("ORGANISATION_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task InviteAsync_ExistingPendingInvite_IsRevoked_And_NewOneCreated()
    {
        var orgId = SeedOrg();
        var ownerId = SeedMember(orgId, OrganisationRole.Owner);

        // First invite
        var first = await _service.InviteAsync(new InviteMemberRequest(
            orgId, ownerId, "Email", SampleHash,
            OrganisationRole.Viewer, SampleTokenHash, DateTime.UtcNow.AddDays(3)));
        Assert.True(first.IsSuccess);

        // Second invite for same invitee
        var second = await _service.InviteAsync(new InviteMemberRequest(
            orgId, ownerId, "Email", SampleHash,
            OrganisationRole.Recruiter, SampleTokenHash, DateTime.UtcNow.AddDays(7)));
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.InvitationId, second.InvitationId);

        // First invitation should be Revoked
        var firstInvite = await _inviteRepo.GetByIdAsync(first.InvitationId!.Value);
        Assert.Equal(InvitationStatus.Revoked, firstInvite!.Status);
    }

    [Fact]
    public async Task InviteAsync_EmptyOrgId_Returns_ValidationFailed()
    {
        var result = await _service.InviteAsync(new InviteMemberRequest(
            Guid.Empty, Guid.NewGuid(), "Email", SampleHash,
            OrganisationRole.Viewer, SampleTokenHash, DateTime.UtcNow.AddDays(3)));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
    }

    // ── Accept ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AcceptAsync_HappyPath_CreatesMembership()
    {
        var orgId = SeedOrg();
        var ownerId = SeedMember(orgId, OrganisationRole.Owner);
        var inviteId = await SeedPendingInvitation(orgId, ownerId, OrganisationRole.Recruiter);
        var acceptorId = Guid.NewGuid();

        var result = await _service.AcceptAsync(new AcceptInvitationRequest(inviteId, acceptorId));

        Assert.True(result.IsSuccess);
        Assert.Equal(orgId, result.OrganisationId);
        Assert.Equal(OrganisationRole.Recruiter, result.Role);

        // Verify membership created
        var membership = await _membershipRepo.GetAsync(orgId, acceptorId);
        Assert.NotNull(membership);
        Assert.Equal(OrganisationRole.Recruiter, membership.Role);
    }

    [Fact]
    public async Task AcceptAsync_AlreadyMember_IsIdempotent()
    {
        var orgId = SeedOrg();
        var ownerId = SeedMember(orgId, OrganisationRole.Owner);
        var inviteId = await SeedPendingInvitation(orgId, ownerId, OrganisationRole.Recruiter);

        // Accept once
        await _service.AcceptAsync(new AcceptInvitationRequest(inviteId, ownerId));

        // Owner already has membership — idempotent result
        var second = await _service.AcceptAsync(new AcceptInvitationRequest(inviteId, ownerId));
        Assert.True(second.IsSuccess);
        Assert.Equal(OrganisationRole.Owner, second.Role);
    }

    [Fact]
    public async Task AcceptAsync_InvitationNotFound_ReturnsError()
    {
        var result = await _service.AcceptAsync(new AcceptInvitationRequest(Guid.NewGuid(), Guid.NewGuid()));
        Assert.False(result.IsSuccess);
        Assert.Equal("INVITATION_NOT_FOUND", result.ErrorCode);
    }

    // ── Revoke ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAsync_HappyPath_RevokesInvitation()
    {
        var orgId = SeedOrg();
        var ownerId = SeedMember(orgId, OrganisationRole.Owner);
        var inviteId = await SeedPendingInvitation(orgId, ownerId, OrganisationRole.Viewer);

        var result = await _service.RevokeAsync(new RevokeInvitationRequest(inviteId, ownerId));

        Assert.True(result.IsSuccess);
        var invite = await _inviteRepo.GetByIdAsync(inviteId);
        Assert.Equal(InvitationStatus.Revoked, invite!.Status);
    }

    [Fact]
    public async Task RevokeAsync_NonAdmin_Returns_RoleNotAllowed()
    {
        var orgId = SeedOrg();
        var ownerId = SeedMember(orgId, OrganisationRole.Owner);
        var recruiterId = SeedMember(orgId, OrganisationRole.Recruiter);
        var inviteId = await SeedPendingInvitation(orgId, ownerId, OrganisationRole.Viewer);

        var result = await _service.RevokeAsync(new RevokeInvitationRequest(inviteId, recruiterId));

        Assert.False(result.IsSuccess);
        Assert.Equal("ROLE_NOT_ALLOWED", result.ErrorCode);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid SeedOrg()
    {
        var org = Organisation.Create("Test Org Ltd", null, null, null, null, null);
        _orgRepo.AddDirect(org);
        return org.Id;
    }

    private Guid SeedMember(Guid orgId, OrganisationRole role)
    {
        var userId = Guid.NewGuid();
        var membership = OrganisationMembership.Create(orgId, userId, role);
        _membershipRepo.AddDirect(membership);
        return userId;
    }

    private async Task<Guid> SeedPendingInvitation(Guid orgId, Guid invitedByUserId, OrganisationRole role)
    {
        var hash = new byte[32];
        var tokenHash = new byte[64];
        Random.Shared.NextBytes(hash);
        Random.Shared.NextBytes(tokenHash);
        var invite = OrganisationInvitation.Create(
            orgId, invitedByUserId, "Email", hash, role, tokenHash, DateTime.UtcNow.AddDays(3));
        await _inviteRepo.AddAsync(invite);
        return invite.Id;
    }

    // ── In-memory fakes ──────────────────────────────────────────────────────

    private sealed class InMemoryOrgRepository : IOrganisationRepository
    {
        private readonly Dictionary<Guid, Organisation> _orgs = new();

        public void AddDirect(Organisation org) => _orgs[org.Id] = org;

        public Task AddAsync(Organisation org, CancellationToken ct = default)
        {
            _orgs[org.Id] = org;
            return Task.CompletedTask;
        }

        public Task<Organisation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            _orgs.TryGetValue(id, out var org);
            return Task.FromResult(org);
        }

        public Task<IReadOnlyList<Organisation>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        {
            var organisations = ids
                .Where(_orgs.ContainsKey)
                .Select(id => _orgs[id])
                .ToList();

            return Task.FromResult<IReadOnlyList<Organisation>>(organisations);
        }

        public Task<IReadOnlyList<Organisation>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Organisation>>(_orgs.Values.ToList());

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryMembershipRepository : IOrganisationMembershipRepository
    {
        private readonly List<OrganisationMembership> _memberships = new();

        public void AddDirect(OrganisationMembership m) => _memberships.Add(m);

        public Task AddAsync(OrganisationMembership m, CancellationToken ct = default)
        {
            _memberships.Add(m);
            return Task.CompletedTask;
        }

        public Task<OrganisationMembership?> GetAsync(Guid orgId, Guid userId, CancellationToken ct = default)
        {
            var m = _memberships.FirstOrDefault(x => x.OrganisationId == orgId && x.UserAccountId == userId);
            return Task.FromResult(m);
        }

        public Task<IReadOnlyList<OrganisationMembership>> GetByUserAccountIdAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OrganisationMembership>>(_memberships.Where(x => x.UserAccountId == userId).ToList());

        public Task<IReadOnlyList<OrganisationMembership>> GetByOrganisationIdAsync(Guid orgId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OrganisationMembership>>(_memberships.Where(x => x.OrganisationId == orgId).ToList());

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryInvitationRepository : IOrganisationInvitationRepository
    {
        private readonly Dictionary<Guid, OrganisationInvitation> _invitations = new();

        public Task AddAsync(OrganisationInvitation inv, CancellationToken ct = default)
        {
            _invitations[inv.Id] = inv;
            return Task.CompletedTask;
        }

        public Task<OrganisationInvitation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            _invitations.TryGetValue(id, out var inv);
            return Task.FromResult(inv);
        }

        public Task<IReadOnlyList<OrganisationInvitation>> GetByOrganisationIdAsync(Guid orgId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OrganisationInvitation>>(
                _invitations.Values.Where(i => i.OrganisationId == orgId).ToList());

        public Task<OrganisationInvitation?> GetPendingByOrgAndInviteeHashAsync(
            Guid orgId, byte[] inviteeIdentifierHash, CancellationToken ct = default)
        {
            var pending = _invitations.Values.FirstOrDefault(i =>
                i.OrganisationId == orgId &&
                i.Status == InvitationStatus.Pending &&
                i.InviteeIdentifierHash.SequenceEqual(inviteeIdentifierHash));
            return Task.FromResult(pending);
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
