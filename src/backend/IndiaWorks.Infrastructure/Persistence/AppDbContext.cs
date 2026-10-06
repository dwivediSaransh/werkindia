using Microsoft.EntityFrameworkCore;
using IndiaWorks.Domain.E00;
using IndiaWorks.Domain.E01;
using IndiaWorks.Domain.E02;
using IndiaWorks.Domain.E03;
using IndiaWorks.Domain.E04;

namespace IndiaWorks.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExampleEntity> ExampleEntities => Set<ExampleEntity>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<LoginIdentifier> LoginIdentifiers => Set<LoginIdentifier>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<OrganisationIdentifier> OrganisationIdentifiers => Set<OrganisationIdentifier>();
    public DbSet<OrganisationMembership> OrganisationMemberships => Set<OrganisationMembership>();
    public DbSet<OrganisationInvitation> OrganisationInvitations => Set<OrganisationInvitation>();
    public DbSet<Geography> Geographies => Set<Geography>();
    public DbSet<TaxonomyVersion> TaxonomyVersions => Set<TaxonomyVersion>();
    public DbSet<JobFamily> JobFamilies => Set<JobFamily>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RoleAlias> RoleAliases => Set<RoleAlias>();
    public DbSet<Specialisation> Specialisations => Set<Specialisation>();
    public DbSet<WorkContext> WorkContexts => Set<WorkContext>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<CredentialType> CredentialTypes => Set<CredentialType>();
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<SubscriptionPlanVersion> SubscriptionPlanVersions => Set<SubscriptionPlanVersion>();
    public DbSet<EntitlementDefinition> EntitlementDefinitions => Set<EntitlementDefinition>();
    public DbSet<PlanEntitlement> PlanEntitlements => Set<PlanEntitlement>();
    public DbSet<OrganisationSubscription> OrganisationSubscriptions => Set<OrganisationSubscription>();
    public DbSet<EntitlementUsageEvent> EntitlementUsageEvents => Set<EntitlementUsageEvent>();
    public DbSet<OrganisationCommercialAccessState> OrganisationCommercialAccessStates => Set<OrganisationCommercialAccessState>();
    public DbSet<OrganisationCandidateUnlock> OrganisationCandidateUnlocks => Set<OrganisationCandidateUnlock>();
    public DbSet<CandidatePaywallEvent> CandidatePaywallEvents => Set<CandidatePaywallEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}