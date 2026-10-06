using IndiaWorks.Application.Features.E04.Provision;
using IndiaWorks.Domain.E04;
using IndiaWorks.Application.Features.E04.Implement;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndiaWorks.Application.Tests.E04
{
public sealed class ProvisionTests
{
    private readonly FakeOrganisationCandidateUnlockRepository _candidateUnlockRepo = new();
    private readonly FakeOrganisationCommercialAccessStateRepository _accessStateRepo = new();
    private readonly FakeCandidatePaywallEventRepository _paywallEventRepo = new();
    private readonly FakeEntitlementEvaluatorRepository _entitlementEvaluatorRepo = new();
    private readonly FakeEntitlementUsageEventRepository _entitlementUsageEventRepo = new();
    private readonly ProvisionCandidateUnlockHandler _handler;

    public ProvisionTests()
    {
        _handler = new ProvisionCandidateUnlockHandler(
            _candidateUnlockRepo,
            _accessStateRepo,
            _paywallEventRepo,
            new EntitlementEvaluator(_entitlementEvaluatorRepo, NullLogger<EntitlementEvaluator>.Instance),
            _entitlementUsageEventRepo,
            _entitlementEvaluatorRepo,
            NullLogger<ProvisionCandidateUnlockHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ExistingUnlock_ReturnsExistingAccessWithoutConsumingQuota()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        // Set up an existing unlock
        var existingUnlock = OrganisationCandidateUnlock.Create(
            organisationId,
            candidateId,
            UnlockSource.FreeAllowance);
        _candidateUnlockRepo.AddUnlock(existingUnlock);

        // Set up access state with some usage
        var accessState = OrganisationCommercialAccessState.Create(
            organisationId,
            freeCandidateUnlockLimit: 5,
            ResetCadence.None);
        accessState.IncrementUsed(); // Used 1
        _accessStateRepo.SetAccessState(accessState);

        // Act
        var result = await _handler.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId),
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(UnlockSource.FreeAllowance.ToString(), result.AccessSource);
        Assert.Equal(5, result.FreeLimit);
        Assert.Equal(1, result.FreeUsed); // Should remain 1, not incremented
        Assert.Equal(4, result.FreeRemaining);
        Assert.False(result.PaywallRequired);

        // Ensure no additional unlock was added
        Assert.Single(_candidateUnlockRepo.Unlocks);
    }

    [Fact]
    public async Task HandleAsync_FirstFreeCandidate_IncrementsFreeUsed()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        // No existing unlock
        // Set up access state with 0 used
        var accessState = OrganisationCommercialAccessState.Create(
            organisationId,
            freeCandidateUnlockLimit: 5,
            ResetCadence.None);
        _accessStateRepo.SetAccessState(accessState);

        // Act
        var result = await _handler.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId),
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(UnlockSource.FreeAllowance.ToString(), result.AccessSource);
        Assert.Equal(5, result.FreeLimit);
        Assert.Equal(1, result.FreeUsed);
        Assert.Equal(4, result.FreeRemaining);
        Assert.False(result.PaywallRequired);

        // Ensure one unlock was added
        Assert.Single(_candidateUnlockRepo.Unlocks);
        Assert.Equal(UnlockSource.FreeAllowance, _candidateUnlockRepo.Unlocks[0].Source);
    }

    [Fact]
    public async Task HandleAsync_FifthFreeCandidate_NoRemaining()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        // Set up 4 existing unlocks
        for (int i = 0; i < 4; i++)
        {
            var unlock = OrganisationCandidateUnlock.Create(
                organisationId,
                Guid.NewGuid(), // different candidate each time
                UnlockSource.FreeAllowance);
            _candidateUnlockRepo.AddUnlock(unlock);
        }

        // Set up access state with 4 used
        var accessState = OrganisationCommercialAccessState.Create(
            organisationId,
            freeCandidateUnlockLimit: 5,
            ResetCadence.None);
        for (int i = 0; i < 4; i++)
        {
            accessState.IncrementUsed();
        }
        _accessStateRepo.SetAccessState(accessState);

        var candidateId = Guid.NewGuid(); // new candidate

        // Act
        var result = await _handler.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId),
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(UnlockSource.FreeAllowance.ToString(), result.AccessSource);
        Assert.Equal(5, result.FreeLimit);
        Assert.Equal(5, result.FreeUsed);
        Assert.Equal(0, result.FreeRemaining);
        Assert.False(result.PaywallRequired);

        // Ensure a fifth unlock was added
        Assert.Equal(5, _candidateUnlockRepo.Unlocks.Count);
        Assert.Equal(UnlockSource.FreeAllowance, _candidateUnlockRepo.Unlocks[4].Source);
    }

    [Fact]
    public async Task HandleAsync_SixthCandidate_PaywallRequired()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        // Set up 5 existing unlocks (used up free quota)
        for (int i = 0; i < 5; i++)
        {
            var unlock = OrganisationCandidateUnlock.Create(
                organisationId,
                Guid.NewGuid(),
                UnlockSource.FreeAllowance);
            _candidateUnlockRepo.AddUnlock(unlock);
        }

        // Set up access state with 5 used (limit 5)
        var accessState = OrganisationCommercialAccessState.Create(
            organisationId,
            freeCandidateUnlockLimit: 5,
            ResetCadence.None);
        for (int i = 0; i < 5; i++)
        {
            accessState.IncrementUsed();
        }
        _accessStateRepo.SetAccessState(accessState);

        // Entitlement evaluator returns not allowed (simulate no paid entitlement)
        _entitlementEvaluatorRepo.SetEntitlementResult(false, false); // IsAllowed = false, IsMetered = false

        var candidateId = Guid.NewGuid(); // new candidate

        // Act
        var result = await _handler.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId),
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.AccessSource);
        Assert.Equal(5, result.FreeLimit);
        Assert.Equal(5, result.FreeUsed);
        Assert.Equal(0, result.FreeRemaining);
        Assert.True(result.PaywallRequired);
        Assert.Null(result.ErrorCode); // Paywall is not an error, just a business rule

        // Ensure a paywall event was added
        Assert.Single(_paywallEventRepo.PaywallEvents);
        Assert.Equal(organisationId, _paywallEventRepo.PaywallEvents[0].OrganisationId);
        Assert.Equal(candidateId, _paywallEventRepo.PaywallEvents[0].CandidateProfileId);
    }

    [Fact]
    public async Task HandleAsync_ReOpenSameCandidate_ReturnsExistingAccess()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        // Set up an existing unlock
        var existingUnlock = OrganisationCandidateUnlock.Create(
            organisationId,
            candidateId,
            UnlockSource.FreeAllowance);
        _candidateUnlockRepo.AddUnlock(existingUnlock);

        // Set up access state with 5 used (so that if we were to create a new unlock, it would go to paywall)
        var accessState = OrganisationCommercialAccessState.Create(
            organisationId,
            freeCandidateUnlockLimit: 5,
            ResetCadence.None);
        for (int i = 0; i < 5; i++)
        {
            accessState.IncrementUsed();
        }
        _accessStateRepo.SetAccessState(accessState);

        // Act
        var result = await _handler.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId),
            CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(UnlockSource.FreeAllowance.ToString(), result.AccessSource);
        Assert.Equal(5, result.FreeLimit);
        Assert.Equal(5, result.FreeUsed); // Should remain 5
        Assert.Equal(0, result.FreeRemaining);
        Assert.False(result.PaywallRequired);

        // Ensure no additional unlock or paywall event was added
        Assert.Single(_candidateUnlockRepo.Unlocks);
        Assert.Empty(_paywallEventRepo.PaywallEvents);
    }

    [Fact]
    public async Task HandleAsync_ConcurrentFifthSlot_OnlyOneConsumesFreeSlot()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        // Set up 4 existing unlocks
        for (int i = 0; i < 4; i++)
        {
            var unlock = OrganisationCandidateUnlock.Create(
                organisationId,
                Guid.NewGuid(),
                UnlockSource.FreeAllowance);
            _candidateUnlockRepo.AddUnlock(unlock);
        }

        // Set up access state with 4 used
        var accessState = OrganisationCommercialAccessState.Create(
            organisationId,
            freeCandidateUnlockLimit: 5,
            ResetCadence.None);
        for (int i = 0; i < 4; i++)
        {
            accessState.IncrementUsed();
        }
        _accessStateRepo.SetAccessState(accessState);

        // Entitlement evaluator returns not allowed (no paid entitlement) for the second request
        _entitlementEvaluatorRepo.SetEntitlementResult(false, false);

        var candidateId1 = Guid.NewGuid();
        var candidateId2 = Guid.NewGuid();

        // Act - first request
        var result1 = await _handler.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId1),
            CancellationToken.None);

        // Act - second request
        var result2 = await _handler.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId2),
            CancellationToken.None);

        // Assert
        // One of them should get the free slot, the other should get paywall.
        // Since we are calling sequentially, the first will get the free slot and the second will hit paywall.
        Assert.True(result1.Success);
        Assert.Equal(UnlockSource.FreeAllowance.ToString(), result1.AccessSource);
        Assert.Equal(5, result1.FreeLimit);
        Assert.Equal(5, result1.FreeUsed);
        Assert.Equal(0, result1.FreeRemaining);
        Assert.False(result1.PaywallRequired);

        Assert.False(result2.Success);
        Assert.Equal(string.Empty, result2.AccessSource);
        Assert.Equal(5, result2.FreeLimit);
        Assert.Equal(5, result2.FreeUsed);
        Assert.Equal(0, result2.FreeRemaining);
        Assert.True(result2.PaywallRequired);

        // Ensure total unlocks: 4 existing + 1 new free = 5
        Assert.Equal(5, _candidateUnlockRepo.Unlocks.Count);
        // Ensure one paywall event
        Assert.Single(_paywallEventRepo.PaywallEvents);
    }

    [Fact]
    public async Task HandleAsync_ValidationFailed_WhenCallerUserIdEmpty()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();

        // Act
        var result = await _handler.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: Guid.Empty, // empty caller
                CandidateProfileId: candidateId),
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Contains("CallerUserId must not be empty", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ValidationFailed_WhenOrganisationIdEmpty()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        // Act
        var result = await _handler.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: Guid.Empty, // empty organisation
                CallerUserId: callerId,
                CandidateProfileId: candidateId),
            CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ORGANISATION_NOT_FOUND", result.ErrorCode);
        Assert.Contains("Organisation commercial access state not found", result.ErrorMessage);
    }
}

// Fakes for the repositories
public class FakeOrganisationCandidateUnlockRepository : IOrganisationCandidateUnlockRepository
{
    public List<OrganisationCandidateUnlock> Unlocks = new();
    public OrganisationCandidateUnlock? UnlockToReturn;

    public void AddUnlock(OrganisationCandidateUnlock unlock) => Unlocks.Add(unlock);

    public Task<OrganisationCandidateUnlock?> GetByOrganisationAndCandidateAsync(
        Guid organisationId,
        Guid candidateProfileId,
        CancellationToken cancellationToken = default)
    {
        var unlock = Unlocks.FirstOrDefault(u =>
            u.OrganisationId == organisationId &&
            u.CandidateProfileId == candidateProfileId);
        return Task.FromResult(unlock);
    }

    public Task AddAsync(OrganisationCandidateUnlock unlock, CancellationToken cancellationToken = default)
    {
        Unlocks.Add(unlock);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class FakeOrganisationCommercialAccessStateRepository : IOrganisationCommercialAccessStateRepository
{
    public OrganisationCommercialAccessState? AccessState;

    public void SetAccessState(OrganisationCommercialAccessState state) => AccessState = state;

    public Task<OrganisationCommercialAccessState?> GetByOrganisationIdAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AccessState);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class FakeCandidatePaywallEventRepository : ICandidatePaywallEventRepository
{
    public List<CandidatePaywallEvent> PaywallEvents = new();

    public Task AddAsync(CandidatePaywallEvent paywallEvent, CancellationToken cancellationToken = default)
    {
        PaywallEvents.Add(paywallEvent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class FakeEntitlementEvaluatorRepository : IEntitlementEvaluatorRepository
{
    public bool IsAllowedResult = true;
    public bool IsMeteredResult = false; // Not used in this handler, but we set it for completeness

    public void SetEntitlementResult(bool isAllowed, bool isMetered)
    {
        IsAllowedResult = isAllowed;
        IsMeteredResult = isMetered;
    }

    public Task<OrganisationSubscription?> GetActiveSubscriptionAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default)
    {
        if (!IsAllowedResult)
        {
            return Task.FromResult<OrganisationSubscription?>(null);
        }

        // For the provision handler, we don't actually use the active subscription directly.
        // The entitlement evaluator is called, but we are faking its result.
        // We return a dummy subscription so that the entitlement evaluator doesn't throw.
        return Task.FromResult<OrganisationSubscription?>(OrganisationSubscription.Create(
            organisationId,
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1)));
    }

    public Task<PlanEntitlement?> GetPlanEntitlementAsync(
        Guid planVersionId,
        string entitlementDefinitionCode,
        CancellationToken cancellationToken = default)
    {
        // Dummy entitlement
        var planId = Guid.NewGuid();
        var version = SubscriptionPlanVersion.Create(planId, BillingInterval.Monthly, "INR", 100m, 0);
        var entitlementDefinition = EntitlementDefinition.Create(
            entitlementDefinitionCode,
            entitlementDefinitionCode,
            EntitlementUnit.Count,
            ResetCadence.None);
        return Task.FromResult<PlanEntitlement?>(PlanEntitlement.Create(
            version,
            entitlementDefinition,
            enabled: IsAllowedResult, // Use the fake's state
            limit: 10m));
    }

    public Task<EntitlementDefinition?> GetEntitlementDefinitionAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<EntitlementDefinition?>(EntitlementDefinition.Create(
            code,
            code,
            EntitlementUnit.Count,
            ResetCadence.None));
    }

    public Task<decimal> GetUsageAsync(
        Guid organisationId,
        string entitlementDefinitionCode,
        DateTime windowStart,
        DateTime windowEnd,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0m);
    }

    public Task<EntitlementEvaluationResult> EvaluateAsync(
        EvaluateEntitlementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (IsAllowedResult)
        {
            return Task.FromResult(EntitlementEvaluationResult.Success(
                limit: 10m,
                used: 0m,
                resetWindowStart: DateTime.UtcNow,
                resetWindowEnd: DateTime.UtcNow.AddMonths(1)));
        }
        else
        {
            return Task.FromResult(EntitlementEvaluationResult.Denied(
                "FORBIDDEN",
                "Entitlement not enabled on current plan."));
        }
    }
}

public class FakeEntitlementUsageEventRepository : IEntitlementUsageEventRepository
{
    public List<EntitlementUsageEvent> UsageEvents = new();

    public Task AddAsync(EntitlementUsageEvent paywallEvent, CancellationToken cancellationToken = default)
    {
        UsageEvents.Add(paywallEvent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
}
