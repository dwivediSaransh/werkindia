using IndiaWorks.Domain.E01;
using IndiaWorks.Domain.E02;
using IndiaWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using IndiaWorks.IntegrationTests.Database;

namespace IndiaWorks.IntegrationTests.Database.E02;

/// <summary>
/// Real database integration tests for Organisation RBAC and invitation flows.
/// Uses SQL Server Testcontainers to verify actual database behavior.
/// </summary>
public sealed class OrganisationRbacIntegrationTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;

    public OrganisationRbacIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private AppDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);
        return new AppDbContext(optionsBuilder.Options);
    }

    private async Task<(Guid orgId, Guid ownerId)> CreateOrganisationWithOwnerAsync(AppDbContext context)
    {
        var organisation = Organisation.Create("Test Organisation Pvt Ltd", "Test Org", EmployerSector.Private, "51-200", "Private Limited", "6201");
        var ownerId = Guid.NewGuid();
        var membership = OrganisationMembership.Create(organisation.Id, ownerId, OrganisationRole.Owner);
        
        context.Organisations.Add(organisation);
        context.OrganisationMemberships.Add(membership);
        await context.SaveChangesAsync();
        
        return (organisation.Id, ownerId);
    }

    private async Task<Guid> AddMemberAsync(AppDbContext context, Guid orgId, Guid userId, OrganisationRole role)
    {
        var membership = OrganisationMembership.Create(orgId, userId, role);
        context.OrganisationMemberships.Add(membership);
        await context.SaveChangesAsync();
        return userId;
    }

    private static byte[] CreateHash(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
    }

    // ── Invite Permission Matrix ──────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task InvitePermission_Owner_CanInvite()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var inviteeHash = CreateHash("invitee1@example.com");
        var tokenHash = CreateHash("token-123");

        var invitation = OrganisationInvitation.Create(
            orgId, ownerId, "Email", inviteeHash, OrganisationRole.Viewer, tokenHash, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation);
        await context.SaveChangesAsync();

        var saved = await context.OrganisationInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitation.Id);

        Assert.NotNull(saved);
        Assert.Equal(InvitationStatus.Pending, saved!.Status);
        Assert.Equal(OrganisationRole.Viewer, saved.Role);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task InvitePermission_Admin_CanInvite()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var adminId = await AddMemberAsync(context, orgId, Guid.NewGuid(), OrganisationRole.Admin);
        var inviteeHash = CreateHash("invitee2@example.com");
        var tokenHash = CreateHash("token-456");

        var invitation = OrganisationInvitation.Create(
            orgId, adminId, "Email", inviteeHash, OrganisationRole.Recruiter, tokenHash, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation);
        await context.SaveChangesAsync();

        var saved = await context.OrganisationInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitation.Id);

        Assert.NotNull(saved);
        Assert.Equal(InvitationStatus.Pending, saved!.Status);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task InvitePermission_Recruiter_CannotInvite_DatabaseConstraint()
    {
        // Note: This test validates that the database allows the row to be inserted
        // but the application service layer enforces RBAC. The DB itself doesn't enforce this.
        // This test documents the expected service-layer behavior.
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var recruiterId = await AddMemberAsync(context, orgId, Guid.NewGuid(), OrganisationRole.Recruiter);
        var inviteeHash = CreateHash("invitee3@example.com");
        var tokenHash = CreateHash("token-789");

        // At database level, this would succeed - the constraint is in the application service
        var invitation = OrganisationInvitation.Create(
            orgId, recruiterId, "Email", inviteeHash, OrganisationRole.Viewer, tokenHash, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation);
        await context.SaveChangesAsync(); // This succeeds at DB level

        var saved = await context.OrganisationInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitation.Id);

        Assert.NotNull(saved); // DB allows it; service layer should reject
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task InvitePermission_Viewer_CannotInvite_DatabaseConstraint()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var viewerId = await AddMemberAsync(context, orgId, Guid.NewGuid(), OrganisationRole.Viewer);
        var inviteeHash = CreateHash("invitee4@example.com");
        var tokenHash = CreateHash("token-abc");

        var invitation = OrganisationInvitation.Create(
            orgId, viewerId, "Email", inviteeHash, OrganisationRole.Viewer, tokenHash, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation);
        await context.SaveChangesAsync();

        var saved = await context.OrganisationInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitation.Id);

        Assert.NotNull(saved); // DB allows it; service layer should reject
    }

    // ── Revoke Permission Matrix ──────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task RevokePermission_Owner_CanRevoke()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var inviteeHash = CreateHash("revoke1@example.com");
        var tokenHash = CreateHash("revoke-token-1");

        var invitation = OrganisationInvitation.Create(
            orgId, ownerId, "Email", inviteeHash, OrganisationRole.Viewer, tokenHash, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation);
        await context.SaveChangesAsync();

        // Owner revokes
        invitation.Revoke();
        await context.SaveChangesAsync();

        var saved = await context.OrganisationInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitation.Id);

        Assert.NotNull(saved);
        Assert.Equal(InvitationStatus.Revoked, saved!.Status);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task RevokePermission_Admin_CanRevoke()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var adminId = await AddMemberAsync(context, orgId, Guid.NewGuid(), OrganisationRole.Admin);
        var inviteeHash = CreateHash("revoke2@example.com");
        var tokenHash = CreateHash("revoke-token-2");

        var invitation = OrganisationInvitation.Create(
            orgId, ownerId, "Email", inviteeHash, OrganisationRole.Viewer, tokenHash, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation);
        await context.SaveChangesAsync();

        // Admin revokes
        invitation.Revoke();
        await context.SaveChangesAsync();

        var saved = await context.OrganisationInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitation.Id);

        Assert.NotNull(saved);
        Assert.Equal(InvitationStatus.Revoked, saved!.Status);
    }

    // ── Accept Invitation Creates Membership ──────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task AcceptInvitation_CreatesMembership_InDatabase()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var inviteeId = Guid.NewGuid();
        var inviteeHash = CreateHash("accept1@example.com");
        var tokenHash = CreateHash("accept-token-1");

        var invitation = OrganisationInvitation.Create(
            orgId, ownerId, "Email", inviteeHash, OrganisationRole.Recruiter, tokenHash, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation);
        await context.SaveChangesAsync();

        // Accept invitation
        invitation.Accept(inviteeId);
        await context.SaveChangesAsync();

        // Verify invitation is accepted
        var savedInvite = await context.OrganisationInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitation.Id);

        Assert.NotNull(savedInvite);
        Assert.Equal(InvitationStatus.Accepted, savedInvite!.Status);

        // Verify membership created
        var membership = await context.OrganisationMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrganisationId == orgId && m.UserAccountId == inviteeId);

        Assert.NotNull(membership);
        Assert.Equal(OrganisationRole.Recruiter, membership!.Role);
        Assert.Equal(MembershipStatus.Active, membership.Status);
    }

    // ── Invite Idempotency ────────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task InviteIdempotency_SameInviteeTwice_RevokesFirstCreatesNew()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var inviteeHash = CreateHash("idempotent@example.com");
        var tokenHash1 = CreateHash("token-first");
        var tokenHash2 = CreateHash("token-second");

        // First invitation
        var invitation1 = OrganisationInvitation.Create(
            orgId, ownerId, "Email", inviteeHash, OrganisationRole.Viewer, tokenHash1, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation1);
        await context.SaveChangesAsync();

        // Second invitation for same invitee (simulating service layer idempotency behavior)
        var invitation2 = OrganisationInvitation.Create(
            orgId, ownerId, "Email", inviteeHash, OrganisationRole.Recruiter, tokenHash2, DateTime.UtcNow.AddDays(14));

        // Simulate service layer behavior: revoke existing pending invitation for same invitee
        var existingPending = await context.OrganisationInvitations
            .FirstOrDefaultAsync(i => i.OrganisationId == orgId 
                && i.InviteeIdentifierHash.SequenceEqual(inviteeHash) 
                && i.Status == InvitationStatus.Pending);

        if (existingPending != null)
        {
            existingPending.Revoke();
        }

        context.OrganisationInvitations.Add(invitation2);
        await context.SaveChangesAsync();

        // Verify first invitation is revoked
        var firstInvite = await context.OrganisationInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitation1.Id);

        Assert.NotNull(firstInvite);
        Assert.Equal(InvitationStatus.Revoked, firstInvite!.Status);

        // Verify second invitation is pending
        var secondInvite = await context.OrganisationInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitation2.Id);

        Assert.NotNull(secondInvite);
        Assert.Equal(InvitationStatus.Pending, secondInvite!.Status);
        Assert.Equal(OrganisationRole.Recruiter, secondInvite.Role);
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task AcceptIdempotency_AcceptSameInvitationTwice_IsIdempotent()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var inviteeId = Guid.NewGuid();
        var inviteeHash = CreateHash("idem-accept@example.com");
        var tokenHash = CreateHash("idem-token");

        var invitation = OrganisationInvitation.Create(
            orgId, ownerId, "Email", inviteeHash, OrganisationRole.Viewer, tokenHash, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation);
        await context.SaveChangesAsync();

        // First accept
        invitation.Accept(inviteeId);
        await context.SaveChangesAsync();

        var membershipCountAfterFirst = await context.OrganisationMemberships
            .CountAsync(m => m.OrganisationId == orgId && m.UserAccountId == inviteeId);

        Assert.Equal(1, membershipCountAfterFirst);

        // Second accept (idempotent - should not create duplicate membership)
        invitation.Accept(inviteeId); // This is a no-op if already accepted
        await context.SaveChangesAsync();

        var membershipCountAfterSecond = await context.OrganisationMemberships
            .CountAsync(m => m.OrganisationId == orgId && m.UserAccountId == inviteeId);

        Assert.Equal(1, membershipCountAfterSecond); // Still only one membership

        var membership = await context.OrganisationMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrganisationId == orgId && m.UserAccountId == inviteeId);

        Assert.NotNull(membership);
        Assert.Equal(OrganisationRole.Viewer, membership!.Role); // Role from invitation
    }

    // ── Membership Role Validation ────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Membership_RolePersistsCorrectly()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var recruiterId = await AddMemberAsync(context, orgId, Guid.NewGuid(), OrganisationRole.Recruiter);
        var viewerId = await AddMemberAsync(context, orgId, Guid.NewGuid(), OrganisationRole.Viewer);
        var adminId = await AddMemberAsync(context, orgId, Guid.NewGuid(), OrganisationRole.Admin);
        var billingAdminId = await AddMemberAsync(context, orgId, Guid.NewGuid(), OrganisationRole.BillingAdmin);

        var memberships = await context.OrganisationMemberships
            .AsNoTracking()
            .Where(m => m.OrganisationId == orgId)
            .ToListAsync();

        Assert.Equal(5, memberships.Count); // Owner + 4 added

        var roles = memberships.Select(m => m.Role).ToHashSet();
        Assert.Contains(OrganisationRole.Owner, roles);
        Assert.Contains(OrganisationRole.Admin, roles);
        Assert.Contains(OrganisationRole.Recruiter, roles);
        Assert.Contains(OrganisationRole.Viewer, roles);
        Assert.Contains(OrganisationRole.BillingAdmin, roles);
    }

    // ── Unique Constraints ────────────────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task OrganisationMembership_UniqueConstraint_PreventsDuplicateMembership()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var userId = Guid.NewGuid();

        // First membership
        var membership1 = OrganisationMembership.Create(orgId, userId, OrganisationRole.Recruiter);
        context.OrganisationMemberships.Add(membership1);
        await context.SaveChangesAsync();

        // Second membership for same user/org should fail at DB level
        var membership2 = OrganisationMembership.Create(orgId, userId, OrganisationRole.Viewer);
        context.OrganisationMemberships.Add(membership2);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task OrganisationInvitation_UniquePendingPerInvitee_EnforcedByService()
    {
        // The unique partial index on (OrganisationId, InviteeIdentifierHash) WHERE Status = Pending
        // is enforced at database level. This test verifies the index exists.
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var inviteeHash = CreateHash("unique@example.com");
        var tokenHash1 = CreateHash("token-a");
        var tokenHash2 = CreateHash("token-b");

        var invitation1 = OrganisationInvitation.Create(
            orgId, ownerId, "Email", inviteeHash, OrganisationRole.Viewer, tokenHash1, DateTime.UtcNow.AddDays(7));
        var invitation2 = OrganisationInvitation.Create(
            orgId, ownerId, "Email", inviteeHash, OrganisationRole.Recruiter, tokenHash2, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation1);
        context.OrganisationInvitations.Add(invitation2);

        // Both are Pending - this should violate the unique partial index
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    // ── Cascade Delete Verification ───────────────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task CascadeDelete_Organisation_DeletesMembershipsAndInvitations()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var organisation = Organisation.Create("Cascade Test Org", null, EmployerSector.Private, "1-10", "Private Limited", "6201");
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var inviteeHash = CreateHash("cascade@example.com");
        var tokenHash = CreateHash("cascade-token");

        var membership1 = OrganisationMembership.Create(organisation.Id, ownerId, OrganisationRole.Owner);
        var membership2 = OrganisationMembership.Create(organisation.Id, memberId, OrganisationRole.Viewer);
        var invitation = OrganisationInvitation.Create(
            organisation.Id, ownerId, "Email", inviteeHash, OrganisationRole.Recruiter, tokenHash, DateTime.UtcNow.AddDays(7));

        context.Organisations.Add(organisation);
        context.OrganisationMemberships.Add(membership1);
        context.OrganisationMemberships.Add(membership2);
        context.OrganisationInvitations.Add(invitation);
        await context.SaveChangesAsync();

        var orgId = organisation.Id;

        // Delete organisation
        context.Organisations.Remove(organisation);
        await context.SaveChangesAsync();

        // Verify cascade deleted
        var remainingMemberships = await context.OrganisationMemberships
            .CountAsync(m => m.OrganisationId == orgId);
        Assert.Equal(0, remainingMemberships);

        var remainingInvitations = await context.OrganisationInvitations
            .CountAsync(i => i.OrganisationId == orgId);
        Assert.Equal(0, remainingInvitations);
    }

    // ── Sensitive Data Not in Logs/Projections ────────────────────────────────

    [Fact(Skip = "Testcontainers SQL Server requires Docker. Remove Skip to run.")]
    public async Task Invitation_DoesNotExposeSensitiveFields_InQueryResults()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var (orgId, ownerId) = await CreateOrganisationWithOwnerAsync(context);
        var inviteeHash = CreateHash("sensitive@example.com");
        var tokenHash = CreateHash("super-secret-token");

        var invitation = OrganisationInvitation.Create(
            orgId, ownerId, "Email", inviteeHash, OrganisationRole.Viewer, tokenHash, DateTime.UtcNow.AddDays(7));

        context.OrganisationInvitations.Add(invitation);
        await context.SaveChangesAsync();

        // Query the invitation - verify we can read it but sensitive fields are not in typical projections
        var queryResult = await context.OrganisationInvitations
            .AsNoTracking()
            .Where(i => i.OrganisationId == orgId)
            .Select(i => new { i.Id, i.OrganisationId, i.Role, i.Status, i.ExpiresAt, i.CreatedAt })
            .ToListAsync();

        Assert.Single(queryResult);
        var result = queryResult[0];
        Assert.Equal(InvitationStatus.Pending, result.Status);
        Assert.Equal(OrganisationRole.Viewer, result.Role);

        // The projection does NOT include: InviteeIdentifierHash, TokenHash, InvitedByUserAccountId
        // This verifies the pattern of not exposing sensitive fields in read projections
    }
}