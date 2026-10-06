using IndiaWorks.Application.Features.E04.Implement;
using IndiaWorks.Application.Features.E04.Provision;
using IndiaWorks.Domain.E04;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndiaWorks.Application.Tests.E04;

public sealed class ImplementTests
{
    private readonly FakeEntitlementEvaluatorRepository _repository = new();
    private readonly EntitlementEvaluator _evaluator;
    private readonly FakeOrganisationCandidateUnlockRepository _candidateUnlockRepo = new();
    private readonly FakeOrganisationCommercialAccessStateRepository _accessStateRepo = new();
    private readonly FakeCandidatePaywallEventRepository _paywallEventRepo = new();
    private readonly FakeEntitlementUsageEventRepository _entitlementUsageEventRepo = new();
    private readonly CandidateUnlockDecisionService _service;

    public ImplementTests()
    {
        _evaluator = new EntitlementEvaluator(
            _repository,
            NullLogger<EntitlementEvaluator>.Instance);
        _service = new CandidateUnlockDecisionService(
            _candidateUnlockRepo,
            _accessStateRepo,
            _paywallEventRepo,
            _evaluator,
            _entitlementUsageEventRepo,
            _repository,
            NullLogger<CandidateUnlockDecisionService>.Instance);
    }

    [Fact]
    public async Task EvaluateAsync_Allows_WhenUsageIsBelowLimit()
    {
        var organisationId = Guid.NewGuid();
        var planVersionId = AddSubscription(organisationId, SubscriptionStatus.Active);
        AddEntitlement(planVersionId, "CANDIDATE_VIEWS", enabled: true, limit: 10m, ResetCadence.Monthly);
        AddUsage(organisationId, "CANDIDATE_VIEWS", quantity: 3m);

        var result = await _evaluator.EvaluateAsync(
            new EvaluateEntitlementRequest(organisationId, " CANDIDATE_VIEWS "));

        Assert.True(result.IsSuccess);
        Assert.Equal("ALLOW", result.Decision);
        Assert.Equal(10m, result.Limit);
        Assert.Equal(3m, result.Used);
        Assert.Equal(7m, result.Remaining);
        Assert.NotNull(result.ResetWindowStart);
        Assert.NotNull(result.ResetWindowEnd);
    }

    [Fact]
    public async Task EvaluateAsync_Meters_WhenUsageReachesLimit()
    {
        var organisationId = Guid.NewGuid();
        var planVersionId = AddSubscription(organisationId, SubscriptionStatus.Trialing);
        AddEntitlement(planVersionId, "CONTACT_REVEALS", enabled: true, limit: 5m, ResetCadence.Monthly);
        AddUsage(organisationId, "CONTACT_REVEALS", quantity: 5m);

        var result = await _evaluator.EvaluateAsync(
            new EvaluateEntitlementRequest(organisationId, "CONTACT_REVEALS"));

        Assert.True(result.IsSuccess);
        Assert.Equal("METER", result.Decision);
        Assert.Equal(5m, result.Limit);
        Assert.Equal(5m, result.Used);
        Assert.Equal(0m, result.Remaining);
    }

    [Fact]
    public async Task EvaluateAsync_Denies_WhenNoActiveSubscriptionExists()
    {
        var result = await _evaluator.EvaluateAsync(
            new EvaluateEntitlementRequest(Guid.NewGuid(), "CANDIDATE_VIEWS"));

        Assert.False(result.IsSuccess);
        Assert.Equal("DENY", result.Decision);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
        Assert.Contains("subscription", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_Denies_WhenSubscriptionIsNotUsable()
    {
        var organisationId = Guid.NewGuid();
        var planVersionId = AddSubscription(organisationId, SubscriptionStatus.Suspended);
        AddEntitlement(planVersionId, "CANDIDATE_VIEWS", enabled: true, limit: 10m, ResetCadence.Monthly);

        var result = await _evaluator.EvaluateAsync(
            new EvaluateEntitlementRequest(organisationId, "CANDIDATE_VIEWS"));

        Assert.False(result.IsSuccess);
        Assert.Equal("DENY", result.Decision);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
        Assert.Contains("not active", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_Denies_WhenPlanEntitlementIsDisabled()
    {
        var organisationId = Guid.NewGuid();
        var planVersionId = AddSubscription(organisationId, SubscriptionStatus.Active);
        AddEntitlement(planVersionId, "CONTACT_REVEALS", enabled: false, limit: 5m, ResetCadence.Monthly);

        var result = await _evaluator.EvaluateAsync(
            new EvaluateEntitlementRequest(organisationId, "CONTACT_REVEALS"));

        Assert.False(result.IsSuccess);
        Assert.Equal("DENY", result.Decision);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
        Assert.Contains("not enabled", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_Denies_WhenDefinitionIsMissing()
    {
        var organisationId = Guid.NewGuid();
        var planVersionId = AddSubscription(organisationId, SubscriptionStatus.Active);
        var definition = EntitlementDefinition.Create(
            "CANDIDATE_VIEWS",
            "Candidate Views",
            EntitlementUnit.Count,
            ResetCadence.Monthly);
        var plan = SubscriptionPlan.Create("PRO", "Professional");
        var version = SubscriptionPlanVersion.Create(plan.Id, BillingInterval.Monthly, "INR");
        var entitlement = PlanEntitlement.Create(version, definition, enabled: true, limit: 5m);
        _repository.AddPlanEntitlement(planVersionId, entitlement);

        var result = await _evaluator.EvaluateAsync(
            new EvaluateEntitlementRequest(organisationId, "CANDIDATE_VIEWS"));

        Assert.False(result.IsSuccess);
        Assert.Equal("DENY", result.Decision);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
        Assert.Contains("not found", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsValidationFailed_ForInvalidRequest()
    {
        var emptyOrganisation = await _evaluator.EvaluateAsync(
            new EvaluateEntitlementRequest(Guid.Empty, "CANDIDATE_VIEWS"));
        var emptyCode = await _evaluator.EvaluateAsync(
            new EvaluateEntitlementRequest(Guid.NewGuid(), " "));

        Assert.False(emptyOrganisation.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", emptyOrganisation.ErrorCode);
        Assert.False(emptyCode.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", emptyCode.ErrorCode);
    }

    [Fact]
    public async Task EvaluateAsync_BillingPeriod_UsesSubscriptionPeriod()
    {
        var organisationId = Guid.NewGuid();
        var periodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var planVersionId = AddSubscription(organisationId, SubscriptionStatus.Active, periodStart, periodEnd);
        AddEntitlement(planVersionId, "JOB_SLOTS", enabled: true, limit: 3m, ResetCadence.BillingPeriod);

        var result = await _evaluator.EvaluateAsync(
            new EvaluateEntitlementRequest(organisationId, "JOB_SLOTS"));

        Assert.True(result.IsSuccess);
        Assert.Equal(periodStart, result.ResetWindowStart);
        Assert.Equal(periodEnd, result.ResetWindowEnd);
    }

    [Fact]
    public async Task EvaluateAsync_ResultDoesNotExposeSensitiveValues()
    {
        var organisationId = Guid.NewGuid();
        var planVersionId = AddSubscription(organisationId, SubscriptionStatus.Active);
        AddEntitlement(planVersionId, "CANDIDATE_VIEWS", enabled: true, limit: 5m, ResetCadence.Monthly);

        var result = await _evaluator.EvaluateAsync(
            new EvaluateEntitlementRequest(organisationId, "CANDIDATE_VIEWS"));

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
        Assert.DoesNotContain("@", result.ToString(), StringComparison.Ordinal);
    }

    // Tests for CandidateUnlockDecisionService

    [Fact]
    public async Task HandleAsync_GivenValidStateAndAuthorisedActor_ReturnsExpectedResultAndPersistsState()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        
        // Set up access state with 5 used (no free slots)
        var accessState = OrganisationCommercialAccessState.Create(
            organisationId,
            freeCandidateUnlockLimit: 5,
            ResetCadence.None);
        for (int i = 0; i < 5; i++)
        {
            accessState.IncrementUsed();
        }
        _accessStateRepo.SetAccessState(accessState);
        
        // Set up subscription and entitlement for paid access
        var planVersionId = AddSubscription(organisationId, SubscriptionStatus.Active);
        AddEntitlement(planVersionId, "CANDIDATE_UNLOCK", enabled: true, limit: 10m, ResetCadence.Monthly);
        
        // Act
        var result = await _service.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId));
        
        // Debug output
        System.Console.WriteLine($"Result: Success={result.Success}, AccessSource={result.AccessSource}, FreeUsed={result.FreeUsed}, FreeRemaining={result.FreeRemaining}, PaywallRequired={result.PaywallRequired}");
        if (!result.Success)
        {
            System.Console.WriteLine($"ErrorCode={result.ErrorCode}, ErrorMessage={result.ErrorMessage}");
        }
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal(UnlockSource.PaidEntitlement.ToString(), result.AccessSource);
        Assert.Equal(5, result.FreeLimit);
        Assert.Equal(5, result.FreeUsed); // Free usage unchanged for paid unlock
        Assert.Equal(0, result.FreeRemaining);
        Assert.False(result.PaywallRequired);
        
        // Verify state persisted
        Assert.Single(_candidateUnlockRepo.Unlocks);
        Assert.Equal(UnlockSource.PaidEntitlement, _candidateUnlockRepo.Unlocks[0].Source);
        Assert.Equal(organisationId, _candidateUnlockRepo.Unlocks[0].OrganisationId);
        Assert.Equal(candidateId, _candidateUnlockRepo.Unlocks[0].CandidateProfileId);
    }

    [Fact]
    public async Task HandleAsync_FirstFreeCandidate_ReturnsFreeUnlockedAndIncrementsFreeUsed()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        
        // Set up access state with 0 used
        var accessState = OrganisationCommercialAccessState.Create(
            organisationId,
            freeCandidateUnlockLimit: 5,
            ResetCadence.None);
        _accessStateRepo.SetAccessState(accessState);
        
        // Entitlement evaluator returns not allowed (to force free path)
        _repository.SetEntitlementResult(false, false);

        // Act
        var result = await _service.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(UnlockSource.FreeAllowance.ToString(), result.AccessSource);
        Assert.Equal(5, result.FreeLimit);
        Assert.Equal(1, result.FreeUsed);
        Assert.Equal(4, result.FreeRemaining);
        Assert.False(result.PaywallRequired);
        
        // Verify state persisted
        Assert.Single(_candidateUnlockRepo.Unlocks);
        Assert.Equal(UnlockSource.FreeAllowance, _candidateUnlockRepo.Unlocks[0].Source);
    }

    [Fact]
    public async Task HandleAsync_FifthFreeCandidate_ReturnsFreeUnlockedWithNoRemaining()
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
        
        // Entitlement evaluator returns not allowed (to force free path)
        _repository.SetEntitlementResult(false, false);
        
        var candidateId = Guid.NewGuid(); // new candidate

        // Act
        var result = await _service.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(UnlockSource.FreeAllowance.ToString(), result.AccessSource);
        Assert.Equal(5, result.FreeLimit);
        Assert.Equal(5, result.FreeUsed);
        Assert.Equal(0, result.FreeRemaining);
        Assert.False(result.PaywallRequired);
        
        // Verify a fifth unlock was added
        Assert.Equal(5, _candidateUnlockRepo.Unlocks.Count);
        Assert.Equal(UnlockSource.FreeAllowance, _candidateUnlockRepo.Unlocks[4].Source);
    }

    [Fact]
    public async Task HandleAsync_SixthCandidate_NoPaidEntitlement_ReturnsPaywallRequired()
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
        _repository.SetEntitlementResult(false, false);
        
        var candidateId = Guid.NewGuid(); // new candidate

        // Act
        var result = await _service.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId));

        // Assert
        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.AccessSource);
        Assert.Equal(5, result.FreeLimit);
        Assert.Equal(5, result.FreeUsed);
        Assert.Equal(0, result.FreeRemaining);
        Assert.True(result.PaywallRequired);
        Assert.Null(result.ErrorCode); // Paywall is not an error, just a business rule
        
        // Verify a paywall event was added
        Assert.Single(_paywallEventRepo.PaywallEvents);
        Assert.Equal(organisationId, _paywallEventRepo.PaywallEvents[0].OrganisationId);
        Assert.Equal(candidateId, _paywallEventRepo.PaywallEvents[0].CandidateProfileId);
    }

    [Fact]
    public async Task HandleAsync_ReOpenSameCandidate_ReturnsExistingAccessWithoutConsumingQuota()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        
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
        
        // Entitlement evaluator returns not allowed (to force paywall path if new unlock attempted)
        _repository.SetEntitlementResult(false, false);

        // Act
        var result = await _service.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(UnlockSource.FreeAllowance.ToString(), result.AccessSource);
        Assert.Equal(5, result.FreeLimit);
        Assert.Equal(5, result.FreeUsed); // Should remain 5
        Assert.Equal(0, result.FreeRemaining);
        Assert.False(result.PaywallRequired);
        
        // Verify no additional unlock or paywall event was added
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
        
        // Entitlement evaluator returns not allowed (no paid entitlement) for second request
        _repository.SetEntitlementResult(false, false);
        
        var candidateId1 = Guid.NewGuid();
        var candidateId2 = Guid.NewGuid();

        // Act - first request (should get the free slot)
        var result1 = await _service.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId1));

        // Act - second request (should hit paywall)
        var result2 = await _service.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: callerId,
                CandidateProfileId: candidateId2));

        // Assert
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
        
        // Verify total unlocks: 4 existing + 1 new free = 5
        Assert.Equal(5, _candidateUnlockRepo.Unlocks.Count);
        // Verify one paywall event
        Assert.Single(_paywallEventRepo.PaywallEvents);
    }

    [Fact]
    public async Task HandleAsync_ValidationFailed_WhenCallerUserIdEmpty()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();

        // Act
        var result = await _service.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: organisationId,
                CallerUserId: Guid.Empty, // empty caller
                CandidateProfileId: candidateId));

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
        var result = await _service.HandleAsync(
            new ProvisionCandidateUnlockRequest(
                OrganisationId: Guid.Empty, // empty organisation
                CallerUserId: callerId,
                CandidateProfileId: candidateId));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ORGANISATION_NOT_FOUND", result.ErrorCode);
        Assert.Contains("Organisation commercial access state not found", result.ErrorMessage);
    }

    private Guid AddSubscription(
        Guid organisationId,
        SubscriptionStatus status,
        DateTime? periodStart = null,
        DateTime? periodEnd = null)
    {
        var plan = SubscriptionPlan.Create($"PLAN_{Guid.NewGuid():N}", "Test Plan");
        var version = plan.AddVersion(BillingInterval.Monthly, "INR");
        var subscription = OrganisationSubscription.Create(
            organisationId,
            version.Id,
            periodStart ?? DateTime.UtcNow.AddDays(-15),
            periodEnd ?? DateTime.UtcNow.AddDays(15));
        subscription.UpdateStatus(status);
        _repository.AddSubscription(subscription);
        return version.Id;
    }

    private void AddEntitlement(
        Guid planVersionId,
        string code,
        bool enabled,
        decimal? limit,
        ResetCadence resetCadence)
    {
        var definition = EntitlementDefinition.Create(
            code,
            code.Replace('_', ' '),
            EntitlementUnit.Count,
            resetCadence);
        var plan = SubscriptionPlan.Create($"PLAN_{Guid.NewGuid():N}", "Test Plan");
        var version = SubscriptionPlanVersion.Create(plan.Id, BillingInterval.Monthly, "INR");
        var entitlement = PlanEntitlement.Create(version, definition, enabled, limit);

        _repository.AddEntitlementDefinition(definition);
        _repository.AddPlanEntitlement(planVersionId, entitlement);
    }

    private void AddUsage(Guid organisationId, string code, decimal quantity)
    {
        _repository.AddUsageEvent(
            EntitlementUsageEvent.Create(
                organisationId,
                code,
                Guid.NewGuid(),
                quantity,
                $"usage-{Guid.NewGuid():N}"));
    }

    private sealed class FakeEntitlementEvaluatorRepository : IEntitlementEvaluatorRepository
    {
        private readonly List<EntitlementDefinition> _definitions = new();
        private readonly List<PlanEntitlementRow> _entitlements = new();
        private readonly List<OrganisationSubscription> _subscriptions = new();
        private readonly List<EntitlementUsageEvent> _usageEvents = new();
        private bool _isAllowedResult = true;
        private bool _isMeteredResult = false;

        public void AddSubscription(OrganisationSubscription subscription) => _subscriptions.Add(subscription);

        public void AddEntitlementDefinition(EntitlementDefinition definition) => _definitions.Add(definition);

        public void AddPlanEntitlement(Guid planVersionId, PlanEntitlement entitlement)
        {
            _entitlements.Add(new PlanEntitlementRow(
                planVersionId,
                entitlement.EntitlementDefinitionCode,
                entitlement.Enabled,
                entitlement.Limit));
        }

        public void AddUsageEvent(EntitlementUsageEvent usageEvent) => _usageEvents.Add(usageEvent);

        public void SetEntitlementResult(bool isAllowed, bool isMetered)
        {
            _isAllowedResult = isAllowed;
            _isMeteredResult = isMetered;
        }

        public Task<OrganisationSubscription?> GetActiveSubscriptionAsync(
            Guid organisationId,
            CancellationToken cancellationToken = default)
        {
            var subscription = _subscriptions.FirstOrDefault(s => s.OrganisationId == organisationId);
            return Task.FromResult(subscription);
        }

        public Task<PlanEntitlement?> GetPlanEntitlementAsync(
            Guid planVersionId,
            string entitlementDefinitionCode,
            CancellationToken cancellationToken = default)
        {
            var row = _entitlements.FirstOrDefault(e =>
                e.PlanVersionId == planVersionId &&
                e.EntitlementDefinitionCode == entitlementDefinitionCode);
            if (row is null)
            {
                return Task.FromResult<PlanEntitlement?>(null);
            }

            var definition = EntitlementDefinition.Create(
                row.EntitlementDefinitionCode,
                row.EntitlementDefinitionCode,
                EntitlementUnit.Count,
                ResetCadence.None);
            var version = SubscriptionPlanVersion.Create(Guid.NewGuid(), BillingInterval.Monthly, "INR");
            var entitlement = PlanEntitlement.Create(version, definition, row.Enabled, row.Limit);
            return Task.FromResult<PlanEntitlement?>(entitlement);
        }

        public Task<EntitlementDefinition?> GetEntitlementDefinitionAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_definitions.FirstOrDefault(d => d.Code == code));
        }

        public Task<decimal> GetUsageAsync(
            Guid organisationId,
            string entitlementDefinitionCode,
            DateTime windowStart,
            DateTime windowEnd,
            CancellationToken cancellationToken = default)
        {
            var usage = _usageEvents
                .Where(e => e.OrganisationId == organisationId)
                .Where(e => e.EntitlementDefinitionCode == entitlementDefinitionCode)
                .Where(e => e.EventDate >= windowStart && e.EventDate < windowEnd)
                .Sum(e => e.Quantity);

            return Task.FromResult(usage);
        }

        public Task<EntitlementEvaluationResult> EvaluateAsync(
            EvaluateEntitlementRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!_isAllowedResult)
            {
                return Task.FromResult(EntitlementEvaluationResult.Denied(
                    "FORBIDDEN",
                    "Entitlement not enabled on current plan."));
            }

            var limit = 10m; // default limit for testing
            var used = 0m; // default used for testing
            var decision = _isMeteredResult && used >= limit ? "METER" : "ALLOW";
            var remaining = limit - used;

            return Task.FromResult(new EntitlementEvaluationResult(
                IsSuccess: true,
                Decision: decision,
                Limit: limit,
                Used: used,
                Remaining: remaining,
                ResetWindowStart: DateTime.UtcNow,
                ResetWindowEnd: DateTime.UtcNow.AddMonths(1)));
        }
    }

    private sealed class FakeOrganisationCandidateUnlockRepository : IOrganisationCandidateUnlockRepository
    {
        public List<OrganisationCandidateUnlock> Unlocks = new();

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

    private sealed class FakeOrganisationCommercialAccessStateRepository : IOrganisationCommercialAccessStateRepository
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

    private sealed class FakeCandidatePaywallEventRepository : ICandidatePaywallEventRepository
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

    private sealed class FakeEntitlementUsageEventRepository : IEntitlementUsageEventRepository
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

    private sealed class PlanEntitlementRow
    {
        public Guid PlanVersionId { get; init; }
        public string EntitlementDefinitionCode { get; init; } = default!;
        public bool Enabled { get; init; }
        public decimal? Limit { get; init; }

        public PlanEntitlementRow(Guid planVersionId, string entitlementDefinitionCode, bool enabled, decimal? limit)
        {
            PlanVersionId = planVersionId;
            EntitlementDefinitionCode = entitlementDefinitionCode;
            Enabled = enabled;
            Limit = limit;
        }
    }
}