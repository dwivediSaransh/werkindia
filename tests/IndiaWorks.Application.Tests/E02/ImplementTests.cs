using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Domain.E02;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndiaWorks.Application.Tests.E02;

public sealed class ImplementTests
{
    private readonly InMemoryOrganisationRepository _orgRepo = new();
    private readonly InMemoryOrganisationMembershipRepository _membershipRepo = new();
    private readonly InMemoryOrganisationInvitationRepository _invitationRepo = new();
    private readonly OrganisationCreationService _creationService;
    private readonly OrganisationMemberInviteService _inviteService;

    public ImplementTests()
    {
        _creationService = new OrganisationCreationService(
            _orgRepo,
            _membershipRepo,
            NullLogger<OrganisationCreationService>.Instance);

        _inviteService = new OrganisationMemberInviteService(
            _orgRepo,
            _membershipRepo,
            _invitationRepo,
            NullLogger<OrganisationMemberInviteService>.Instance);
    }

    // ── Organisation creation tests (unchanged) ───────────────────────────────

    [Fact]
    public async Task CreateAsync_HappyPath_CreatesOrganisationAndOwnerMembership()
    {
        var creatorId = Guid.NewGuid();
        var request = new CreateOrganisationRequest(
            CreatorUserAccountId: creatorId,
            LegalName: "  Acme   Technologies   Pvt   Ltd  ",
            TradeName: "  Acme   Tech  ",
            Sector: EmployerSector.Private,
            EmployeeBand: "51-200",
            OrganisationType: "Private Limited",
            Nic2025Code: "6201");

        var result = await _creationService.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.OrganisationId);
        Assert.Equal("Acme Technologies Pvt Ltd", result.LegalName);
        Assert.Equal("Acme Tech", result.TradeName);
        Assert.Equal(EmployerSector.Private, result.Sector);
        Assert.Equal("51-200", result.EmployeeBand);
        Assert.Equal(VerificationStatus.Unverified, result.VerificationStatus);
        Assert.Equal(OrganisationRole.Owner, result.Role);

        var persistedOrg = await _orgRepo.GetByIdAsync(result.OrganisationId.Value);
        Assert.NotNull(persistedOrg);
        Assert.Equal("Acme Technologies Pvt Ltd", persistedOrg.LegalName);

        var persistedMembership = await _membershipRepo.GetAsync(result.OrganisationId.Value, creatorId);
        Assert.NotNull(persistedMembership);
        Assert.Equal(OrganisationRole.Owner, persistedMembership.Role);
        Assert.Equal(MembershipStatus.Active, persistedMembership.Status);
    }

    [Fact]
    public async Task CreateAsync_EmptyCreatorId_ReturnsValidationFailed()
    {
        var request = new CreateOrganisationRequest(
            CreatorUserAccountId: Guid.Empty,
            LegalName: "Valid Legal Name");

        var result = await _creationService.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Contains("CreatorUserAccountId", result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_InvalidLegalName_ReturnsValidationFailed(string? invalidName)
    {
        var request = new CreateOrganisationRequest(
            CreatorUserAccountId: Guid.NewGuid(),
            LegalName: invalidName!);

        var result = await _creationService.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Contains("LegalName", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateAsync_NormalizesWhitespaceInNames()
    {
        var request = new CreateOrganisationRequest(
            CreatorUserAccountId: Guid.NewGuid(),
            LegalName: "  Alpha \t Beta \n  Gamma  ",
            TradeName: "  Alpha \t Beta  ");

        var result = await _creationService.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alpha Beta Gamma", result.LegalName);
        Assert.Equal("Alpha Beta", result.TradeName);
    }

    [Fact]
    public async Task CreateAsync_CommitsOrganisationAndMembershipInOneSave()
    {
        var request = new CreateOrganisationRequest(
            CreatorUserAccountId: Guid.NewGuid(),
            LegalName: "One Transaction Pvt Ltd");

        var result = await _creationService.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _orgRepo.SaveChangesCount);
        Assert.Equal(0, _membershipRepo.SaveChangesCount);
        Assert.Single(_orgRepo.Organisations);
        Assert.Single(_membershipRepo.Memberships);
    }

    // ── Invite / accept / revoke tests ───────────────────────────────────────

    [Fact]
    public async Task InviteAsync_HappyPath_CreatesPendingInvitation()
    {
        var inviterId = Guid.NewGuid();
        var inviteeHash = RandomHasher.Compute("invitee@example.com");

        var orgId = await CreateOrganisationAndOwnerMembership(inviterId);

        var request = new InviteMemberRequest(
            OrganisationId: orgId,
            InvitedByUserAccountId: inviterId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: inviteeHash,
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var result = await _inviteService.InviteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorCode);
        Assert.NotEqual(Guid.Empty, result.InvitationId!.Value);
        var invitation = await _invitationRepo.GetByIdAsync(result.InvitationId.Value);
        Assert.NotNull(invitation);
        Assert.Equal(orgId, invitation.OrganisationId);
        Assert.Equal(inviterId, invitation.InvitedByUserAccountId);
        Assert.Equal("Email", invitation.InviteeIdentifierType);
        Assert.Equal(OrganisationRole.Recruiter, invitation.Role);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.True(invitation.IsActive);
    }

    [Fact]
    public async Task InviteAsync_NonExistentOrganisation_ReturnsOrganisationNotFound()
    {
        var request = new InviteMemberRequest(
            OrganisationId: Guid.NewGuid(),
            InvitedByUserAccountId: Guid.NewGuid(),
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: RandomHasher.Compute("a@b.com"),
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var result = await _inviteService.InviteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("ORGANISATION_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task InviteAsync_CallerNotActiveMember_ReturnsMembershipRequired()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var inviteeHash = RandomHasher.Compute("a@b.com");

        var organisationId = await CreateOrganisationAndOwnerMembership(ownerId);

        var request = new InviteMemberRequest(
            OrganisationId: organisationId,
            InvitedByUserAccountId: callerId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: inviteeHash,
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var result = await _inviteService.InviteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("MEMBERSHIP_REQUIRED", result.ErrorCode);
    }

    [Fact]
    public async Task InviteAsync_CallerNotOwnerOrAdmin_ReturnsRoleNotAllowed()
    {
        var inviterId = Guid.NewGuid();
        var inviteeHash = RandomHasher.Compute("invitee@example.com");

        // Create org but do NOT add Owner membership — we want inviter to be only Recruiter.
        var org = Organisation.Create("Test Org", "Test", EmployerSector.Private, "1-10", "Private Limited", "6201");
        var orgId = org.Id;
        await _orgRepo.AddAsync(org);
        await _orgRepo.SaveChangesAsync();

        // Now add only a Recruiter membership (Active).
        await AddMembershipToOrg(orgId, inviterId, OrganisationRole.Recruiter, MembershipStatus.Active);

        var request = new InviteMemberRequest(
            OrganisationId: orgId,
            InvitedByUserAccountId: inviterId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: inviteeHash,
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var result = await _inviteService.InviteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("ROLE_NOT_ALLOWED", result.ErrorCode);
    }

    [Fact]
    public async Task InviteAsync_RevokesExistingPendingInvitationForSameInvitee()
    {
        var inviterId = Guid.NewGuid();
        var inviteeHash = RandomHasher.Compute("same@invitee.com");

        var orgId = await CreateOrganisationAndOwnerMembership(inviterId);

        var firstRequest = new InviteMemberRequest(
            OrganisationId: orgId,
            InvitedByUserAccountId: inviterId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: inviteeHash,
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token1"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var firstResult = await _inviteService.InviteAsync(firstRequest);
        Assert.True(firstResult.IsSuccess);

        var secondRequest = new InviteMemberRequest(
            OrganisationId: orgId,
            InvitedByUserAccountId: inviterId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: inviteeHash,
            Role: OrganisationRole.Admin,
            TokenHash: RandomHasher.Compute("token2"),
            ExpiresAt: DateTime.UtcNow.AddDays(14));

        var secondResult = await _inviteService.InviteAsync(secondRequest);
        Assert.True(secondResult.IsSuccess);
        Assert.NotEqual(firstResult.InvitationId, secondResult.InvitationId);

        var existingPending = await _invitationRepo.GetPendingByOrgAndInviteeHashAsync(orgId, inviteeHash);
        Assert.NotNull(existingPending);
        Assert.Equal(secondResult.InvitationId, existingPending.Id);
    }

    [Fact]
    public async Task AcceptAsync_HappyPath_CreatesMembershipAndReturnsOrganisationAndRole()
    {
        var inviterId = Guid.NewGuid();
        var acceptorId = Guid.NewGuid();
        var inviteeHash = RandomHasher.Compute("invitee@example.com");

        var organisationId = await CreateOrganisationAndOwnerMembership(inviterId);

        var inviteRequest = new InviteMemberRequest(
            OrganisationId: organisationId,
            InvitedByUserAccountId: inviterId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: inviteeHash,
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var inviteResult = await _inviteService.InviteAsync(inviteRequest);
        Assert.True(inviteResult.IsSuccess);

        var acceptRequest = new AcceptInvitationRequest(
            InvitationId: inviteResult.InvitationId!.Value,
            AcceptedByUserAccountId: acceptorId);

        var acceptResult = await _inviteService.AcceptAsync(acceptRequest);

        Assert.True(acceptResult.IsSuccess);
        Assert.Equal(organisationId, acceptResult.OrganisationId!.Value);
        Assert.Equal(OrganisationRole.Recruiter, acceptResult.Role!.Value);

        var membership = await _membershipRepo.GetAsync(organisationId, acceptorId);
        Assert.NotNull(membership);
        Assert.Equal(OrganisationRole.Recruiter, membership.Role);
        Assert.Equal(MembershipStatus.Active, membership.Status);

        var acceptedInvitation = await _invitationRepo.GetByIdAsync(inviteResult.InvitationId.Value);
        Assert.NotNull(acceptedInvitation);
        Assert.Equal(InvitationStatus.Accepted, acceptedInvitation.Status);
        Assert.Equal(acceptorId, acceptedInvitation.AcceptedByUserAccountId);
    }

    [Fact]
    public async Task AcceptAsync_Idempotent_WhenUserAlreadyActiveInOrg()
    {
        var inviterId = Guid.NewGuid();
        var acceptorId = Guid.NewGuid();
        var inviteeHash = RandomHasher.Compute("invitee@example.com");

        // Create org with only inviter as Owner; acceptor has no membership yet.
        var organisationId = await CreateOrganisationAndOwnerMembership(inviterId);

        // Add a single active Recruiter membership for the acceptor (no Owner membership for them).
        await AddMembershipToOrg(organisationId, acceptorId, OrganisationRole.Recruiter, MembershipStatus.Active);

        var inviteRequest = new InviteMemberRequest(
            OrganisationId: organisationId,
            InvitedByUserAccountId: inviterId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: inviteeHash,
            Role: OrganisationRole.Admin,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var inviteResult = await _inviteService.InviteAsync(inviteRequest);
        Assert.True(inviteResult.IsSuccess);

        var acceptRequest = new AcceptInvitationRequest(
            InvitationId: inviteResult.InvitationId!.Value,
            AcceptedByUserAccountId: acceptorId);

        var acceptResult = await _inviteService.AcceptAsync(acceptRequest);

        Assert.True(acceptResult.IsSuccess);
        Assert.Equal(organisationId, acceptResult.OrganisationId!.Value);
        Assert.Equal(OrganisationRole.Recruiter, acceptResult.Role!.Value);

        var memberships = await _membershipRepo.GetByOrganisationIdAsync(organisationId);
        Assert.Equal(2, memberships.Count);
        Assert.Equal(1, memberships.Count(m => m.UserAccountId == acceptorId && m.Role == OrganisationRole.Recruiter));
    }

    [Fact]
    public async Task AcceptAsync_NonExistentInvitation_ReturnsInvitationNotFound()
    {
        var request = new AcceptInvitationRequest(
            InvitationId: Guid.NewGuid(),
            AcceptedByUserAccountId: Guid.NewGuid());

        var result = await _inviteService.AcceptAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVITATION_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task AcceptAsync_AlreadyAcceptedInvitation_ReturnsInvitationNotActive()
    {
        var inviterId = Guid.NewGuid();
        var acceptorId = Guid.NewGuid();
        var inviteeHash = RandomHasher.Compute("invitee@example.com");

        var organisationId = await CreateOrganisationAndOwnerMembership(inviterId);

        var inviteRequest = new InviteMemberRequest(
            OrganisationId: organisationId,
            InvitedByUserAccountId: inviterId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: inviteeHash,
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var inviteResult = await _inviteService.InviteAsync(inviteRequest);
        Assert.True(inviteResult.IsSuccess);

        await _inviteService.AcceptAsync(new AcceptInvitationRequest(
            InvitationId: inviteResult.InvitationId!.Value,
            AcceptedByUserAccountId: acceptorId));

        var secondAcceptResult = await _inviteService.AcceptAsync(new AcceptInvitationRequest(
            InvitationId: inviteResult.InvitationId!.Value,
            AcceptedByUserAccountId: Guid.NewGuid()));

        Assert.False(secondAcceptResult.IsSuccess);
        Assert.Equal("INVITATION_NOT_ACTIVE", secondAcceptResult.ErrorCode);
    }

    [Fact]
    public async Task RevokeAsync_HappyPath_RevokesPendingInvitation()
    {
        var ownerId = Guid.NewGuid();
        var inviteeHash = RandomHasher.Compute("invitee@example.com");

        var organisationId = await CreateOrganisationAndOwnerMembership(ownerId);

        var inviteRequest = new InviteMemberRequest(
            OrganisationId: organisationId,
            InvitedByUserAccountId: ownerId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: inviteeHash,
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var inviteResult = await _inviteService.InviteAsync(inviteRequest);
        Assert.True(inviteResult.IsSuccess);

        var revokeRequest = new RevokeInvitationRequest(
            InvitationId: inviteResult.InvitationId!.Value,
            RevokedByUserAccountId: ownerId);

        var revokeResult = await _inviteService.RevokeAsync(revokeRequest);

        Assert.True(revokeResult.IsSuccess);
        var invitation = await _invitationRepo.GetByIdAsync(inviteResult.InvitationId.Value);
        Assert.NotNull(invitation);
        Assert.Equal(InvitationStatus.Revoked, invitation.Status);
        Assert.NotNull(invitation.RevokedAt);
    }

    [Fact]
    public async Task RevokeAsync_NonExistentInvitation_ReturnsInvitationNotFound()
    {
        var request = new RevokeInvitationRequest(
            InvitationId: Guid.NewGuid(),
            RevokedByUserAccountId: Guid.NewGuid());

        var result = await _inviteService.RevokeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVITATION_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task RevokeAsync_CallerNotActiveMember_ReturnsMembershipRequired()
    {
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var inviteeHash = RandomHasher.Compute("invitee@example.com");

        var organisationId = await CreateOrganisationAndOwnerMembership(ownerId);

        var inviteRequest = new InviteMemberRequest(
            OrganisationId: organisationId,
            InvitedByUserAccountId: ownerId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: inviteeHash,
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var inviteResult = await _inviteService.InviteAsync(inviteRequest);
        Assert.True(inviteResult.IsSuccess);

        var revokeRequest = new RevokeInvitationRequest(
            InvitationId: inviteResult.InvitationId!.Value,
            RevokedByUserAccountId: outsiderId);

        var revokeResult = await _inviteService.RevokeAsync(revokeRequest);

        Assert.False(revokeResult.IsSuccess);
        Assert.Equal("MEMBERSHIP_REQUIRED", revokeResult.ErrorCode);
    }

    [Fact]
    public async Task AcceptAsync_EmptyInvitationId_ReturnsValidationFailed()
    {
        var request = new AcceptInvitationRequest(InvitationId: Guid.Empty, AcceptedByUserAccountId: Guid.NewGuid());
        var result = await _inviteService.AcceptAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Contains("InvitationId", result.ErrorMessage);
    }

    [Fact]
    public async Task AcceptAsync_EmptyAcceptedByUserAccountId_ReturnsValidationFailed()
    {
        var request = new AcceptInvitationRequest(InvitationId: Guid.NewGuid(), AcceptedByUserAccountId: Guid.Empty);
        var result = await _inviteService.AcceptAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Contains("AcceptedByUserAccountId", result.ErrorMessage);
    }

    [Fact]
    public async Task RevokeAsync_EmptyInvitationId_ReturnsValidationFailed()
    {
        var request = new RevokeInvitationRequest(InvitationId: Guid.Empty, RevokedByUserAccountId: Guid.NewGuid());
        var result = await _inviteService.RevokeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Contains("InvitationId", result.ErrorMessage);
    }

    [Fact]
    public async Task RevokeAsync_EmptyRevokedByUserAccountId_ReturnsValidationFailed()
    {
        var request = new RevokeInvitationRequest(InvitationId: Guid.NewGuid(), RevokedByUserAccountId: Guid.Empty);
        var result = await _inviteService.RevokeAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Contains("RevokedByUserAccountId", result.ErrorMessage);
    }

    [Fact]
    public async Task InviteAsync_BlankInviteeIdentifierType_ReturnsValidationFailed()
    {
        var inviterId = Guid.NewGuid();

        var orgId = await CreateOrganisationAndOwnerMembership(inviterId);

        var request = new InviteMemberRequest(
            OrganisationId: orgId,
            InvitedByUserAccountId: inviterId,
            InviteeIdentifierType: "   ",
            InviteeIdentifierHash: RandomHasher.Compute("a@b.com"),
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(7));

        var result = await _inviteService.InviteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Contains("InviteeIdentifierType", result.ErrorMessage);
    }

    [Fact]
    public async Task InviteAsync_PastExpiry_ReturnsValidationFailed()
    {
        var inviterId = Guid.NewGuid();

        var orgId = await CreateOrganisationAndOwnerMembership(inviterId);

        var request = new InviteMemberRequest(
            OrganisationId: orgId,
            InvitedByUserAccountId: inviterId,
            InviteeIdentifierType: "Email",
            InviteeIdentifierHash: RandomHasher.Compute("a@b.com"),
            Role: OrganisationRole.Recruiter,
            TokenHash: RandomHasher.Compute("token"),
            ExpiresAt: DateTime.UtcNow.AddDays(-1));

        var result = await _inviteService.InviteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Contains("ExpiresAt", result.ErrorMessage);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> CreateOrganisationAndOwnerMembership(Guid userId)
    {
        var org = Organisation.Create("Test Org", "Test", EmployerSector.Private, "1-10", "Private Limited", "6201");
        await _orgRepo.AddAsync(org);

        var membership = OrganisationMembership.Create(org.Id, userId, OrganisationRole.Owner);
        await _membershipRepo.AddAsync(membership);

        await _orgRepo.SaveChangesAsync();
        return org.Id;
    }

    private async Task<(Guid OrgId, OrganisationMembership Membership)> AddMembershipToOrg(
        Guid orgId, Guid userId, OrganisationRole role, MembershipStatus status)
    {
        var membership = OrganisationMembership.Create(orgId, userId, role);
        if (status != MembershipStatus.Active)
        {
            if (status == MembershipStatus.Suspended)
                membership.Suspend();
            else
                membership.Terminate();
        }
        await _membershipRepo.AddAsync(membership);
        await _orgRepo.SaveChangesAsync();
        return (orgId, membership);
    }

    // ── In-memory fakes ───────────────────────────────────────────────────────

    private sealed class InMemoryOrganisationRepository : IOrganisationRepository
    {
        private readonly Dictionary<Guid, Organisation> _organisations = new();
        public int SaveChangesCount { get; private set; }
        public IReadOnlyCollection<Organisation> Organisations => _organisations.Values.ToList();

        public Task AddAsync(Organisation organisation, CancellationToken cancellationToken = default)
        {
            _organisations[organisation.Id] = organisation;
            return Task.CompletedTask;
        }

        public Task<Organisation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _organisations.TryGetValue(id, out var org);
            return Task.FromResult(org);
        }

        public Task<IReadOnlyList<Organisation>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        {
            var organisations = ids
                .Where(_organisations.ContainsKey)
                .Select(id => _organisations[id])
                .ToList();

            return Task.FromResult<IReadOnlyList<Organisation>>(organisations);
        }

        public Task<IReadOnlyList<Organisation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Organisation>>(_organisations.Values.ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryOrganisationMembershipRepository : IOrganisationMembershipRepository
    {
        private readonly List<OrganisationMembership> _memberships = new();
        public int SaveChangesCount { get; private set; }
        public IReadOnlyList<OrganisationMembership> Memberships => _memberships;

        public Task AddAsync(OrganisationMembership membership, CancellationToken cancellationToken = default)
        {
            _memberships.Add(membership);
            return Task.CompletedTask;
        }

        public Task<OrganisationMembership?> GetAsync(Guid organisationId, Guid userAccountId, CancellationToken cancellationToken = default)
        {
            var m = _memberships.FirstOrDefault(x => x.OrganisationId == organisationId && x.UserAccountId == userAccountId);
            return Task.FromResult(m);
        }

        public Task<IReadOnlyList<OrganisationMembership>> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken = default)
        {
            var list = _memberships.Where(x => x.UserAccountId == userAccountId).ToList();
            return Task.FromResult<IReadOnlyList<OrganisationMembership>>(list);
        }

        public Task<IReadOnlyList<OrganisationMembership>> GetByOrganisationIdAsync(Guid organisationId, CancellationToken cancellationToken = default)
        {
            var list = _memberships.Where(x => x.OrganisationId == organisationId).ToList();
            return Task.FromResult<IReadOnlyList<OrganisationMembership>>(list);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryOrganisationInvitationRepository : IOrganisationInvitationRepository
    {
        private readonly Dictionary<Guid, OrganisationInvitation> _invitations = new();
        public int SaveChangesCount { get; private set; }

        public Task AddAsync(OrganisationInvitation invitation, CancellationToken cancellationToken = default)
        {
            _invitations[invitation.Id] = invitation;
            return Task.CompletedTask;
        }

        public Task<OrganisationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_invitations.TryGetValue(id, out var inv) ? inv : null);
        }

        public Task<IReadOnlyList<OrganisationInvitation>> GetByOrganisationIdAsync(
            Guid organisationId, CancellationToken cancellationToken = default)
        {
            var list = _invitations.Values
                .Where(i => i.OrganisationId == organisationId)
                .ToList();
            return Task.FromResult<IReadOnlyList<OrganisationInvitation>>(list);
        }

        public Task<OrganisationInvitation?> GetPendingByOrgAndInviteeHashAsync(
            Guid organisationId, byte[] inviteeIdentifierHash, CancellationToken cancellationToken = default)
        {
            var match = _invitations.Values
                .FirstOrDefault(i =>
                    i.OrganisationId == organisationId
                    && i.Status == InvitationStatus.Pending
                    && i.InviteeIdentifierHash.SequenceEqual(inviteeIdentifierHash));
            return Task.FromResult(match);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RandomHasher
    {
        public static byte[] Compute(string value)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
        }
    }
}
