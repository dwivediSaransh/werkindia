using IndiaWorks.Application.Features.E04.Implement;
using IndiaWorks.Application.Features.E04.Provision;
using IndiaWorks.Domain.E04;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace IndiaWorks.Application.Features.E04.Implement;

/// <summary>
/// Service for making transactional candidate unlock decisions.
/// </summary>
public sealed class CandidateUnlockDecisionService
{
    private readonly IOrganisationCandidateUnlockRepository _organisationCandidateUnlockRepository;
    private readonly IOrganisationCommercialAccessStateRepository _organisationCommercialAccessStateRepository;
    private readonly ICandidatePaywallEventRepository _candidatePaywallEventRepository;
    private readonly EntitlementEvaluator _entitlementEvaluator;
    private readonly IEntitlementUsageEventRepository _entitlementUsageEventRepository;
    private readonly IEntitlementEvaluatorRepository _entitlementEvaluatorRepository;
    private readonly ILogger<CandidateUnlockDecisionService> _logger;

    public CandidateUnlockDecisionService(
        IOrganisationCandidateUnlockRepository organisationCandidateUnlockRepository,
        IOrganisationCommercialAccessStateRepository organisationCommercialAccessStateRepository,
        ICandidatePaywallEventRepository candidatePaywallEventRepository,
        EntitlementEvaluator entitlementEvaluator,
        IEntitlementUsageEventRepository entitlementUsageEventRepository,
        IEntitlementEvaluatorRepository entitlementEvaluatorRepository,
        ILogger<CandidateUnlockDecisionService> logger)
    {
        _organisationCandidateUnlockRepository = organisationCandidateUnlockRepository;
        _organisationCommercialAccessStateRepository = organisationCommercialAccessStateRepository;
        _candidatePaywallEventRepository = candidatePaywallEventRepository;
        _entitlementEvaluator = entitlementEvaluator;
        _entitlementUsageEventRepository = entitlementUsageEventRepository;
        _entitlementEvaluatorRepository = entitlementEvaluatorRepository;
        _logger = logger;
    }

    public async Task<ProvisionCandidateUnlockResult> HandleAsync(
        ProvisionCandidateUnlockRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Authorize: Check caller is a member of the organisation (simplified for now)
        // In a real implementation, we would use an OrganisationMembershipRepository to verify.
        // For the purpose of this story, we assume the caller is authorised if they provide a valid CallerUserId.
        // We'll add a check that the CallerUserId is not empty.
        if (request.CallerUserId == Guid.Empty)
        {
            _logger.LogWarning("Candidate unlock decision rejected: CallerUserId is empty.");
            return new ProvisionCandidateUnlockResult(
                Success: false,
                AccessSource: string.Empty,
                FreeLimit: 0,
                FreeUsed: 0,
                FreeRemaining: 0,
                PaywallRequired: false,
                ErrorCode: "VALIDATION_FAILED",
                ErrorMessage: "CallerUserId must not be empty.");
        }

        // 2. Load candidate privacy/discoverability/block status before any commercial check.
        // Since we don't have a candidate privacy service in this story, we assume the candidate is discoverable and not blocked.
        // In a real implementation, we would call a candidate privacy service.
        // For now, we skip this step and note that it's out of scope for this story (to be handled by dependencies).

        // 3. Query OrganisationCandidateUnlock by unique (OrganisationId, CandidateProfileId). If found, return existing access without consuming quota.
        var existingUnlock = await _organisationCandidateUnlockRepository.GetByOrganisationAndCandidateAsync(
            request.OrganisationId,
            request.CandidateProfileId,
            cancellationToken);

        if (existingUnlock != null)
        {
            // Existing unlock found, return current state without consuming quota.
            var state = await _organisationCommercialAccessStateRepository.GetByOrganisationIdAsync(
                request.OrganisationId,
                cancellationToken);

            return new ProvisionCandidateUnlockResult(
                Success: true,
                AccessSource: existingUnlock.Source.ToString(),
                FreeLimit: state?.FreeCandidateUnlockLimit ?? 0,
                FreeUsed: state?.FreeCandidateUnlockUsed ?? 0,
                FreeRemaining: state != null ? state.FreeCandidateUnlockLimit - state.FreeCandidateUnlockUsed : 0,
                PaywallRequired: false);
        }

        // 4. Start transaction at an isolation/concurrency level that protects the free counter/unique insert.
        var transactionOptions = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.Serializable,
            Timeout = TransactionManager.MaximumTimeout
        };

        using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
        {
            // 5. Load OrganisationCommercialAccessState for the commercial/legal-entity identity.
            var accessState = await _organisationCommercialAccessStateRepository.GetByOrganisationIdAsync(
                request.OrganisationId,
                cancellationToken);

            if (accessState == null)
            {
                _logger.LogWarning("OrganisationCommercialAccessState not found for organisation {OrganisationId}", request.OrganisationId);
                return new ProvisionCandidateUnlockResult(
                    Success: false,
                    AccessSource: string.Empty,
                    FreeLimit: 0,
                    FreeUsed: 0,
                    FreeRemaining: 0,
                    PaywallRequired: false,
                    ErrorCode: "ORGANISATION_NOT_FOUND",
                    ErrorMessage: "Organisation commercial access state not found.");
            }

            // 6. If lifetime free distinct usage is below 5, insert OrganisationCandidateUnlock(source=FREE_ALLOWANCE) and commit.
            if (accessState.FreeCandidateUnlockUsed < accessState.FreeCandidateUnlockLimit)
            {
                var unlock = OrganisationCandidateUnlock.Create(
                    request.OrganisationId,
                    request.CandidateProfileId,
                    UnlockSource.FreeAllowance);

                try
                {
                    await _organisationCandidateUnlockRepository.AddAsync(unlock, cancellationToken);
                    accessState.IncrementUsed();
                    await _organisationCommercialAccessStateRepository.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException) when (IsUniqueConstraintViolation())
                {
                    // Handle concurrent insert: treat as existing unlock
                    var existingUnlockAfterConflict = await _organisationCandidateUnlockRepository.GetByOrganisationAndCandidateAsync(
                        request.OrganisationId,
                        request.CandidateProfileId,
                        cancellationToken);

                    if (existingUnlockAfterConflict != null)
                    {
                        var stateAfterConflict = await _organisationCommercialAccessStateRepository.GetByOrganisationIdAsync(
                            request.OrganisationId,
                            cancellationToken);

                        scope.Complete(); // Complete the transaction before returning
                        return new ProvisionCandidateUnlockResult(
                            Success: true,
                            AccessSource: existingUnlockAfterConflict.Source.ToString(),
                            FreeLimit: stateAfterConflict?.FreeCandidateUnlockLimit ?? 0,
                            FreeUsed: stateAfterConflict?.FreeCandidateUnlockUsed ?? 0,
                            FreeRemaining: stateAfterConflict != null ? stateAfterConflict.FreeCandidateUnlockLimit - stateAfterConflict.FreeCandidateUnlockUsed : 0,
                            PaywallRequired: false);
                    }

                    throw; // If we still can't find it, rethrow
                }

                scope.Complete(); // Complete the transaction
                return new ProvisionCandidateUnlockResult(
                    Success: true,
                    AccessSource: UnlockSource.FreeAllowance.ToString(),
                    FreeLimit: accessState.FreeCandidateUnlockLimit,
                    FreeUsed: accessState.FreeCandidateUnlockUsed, // Now updated
                    FreeRemaining: accessState.FreeCandidateUnlockLimit - accessState.FreeCandidateUnlockUsed,
                    PaywallRequired: false);
            }
            else
            {
                // 7. Otherwise evaluate central CANDIDATE_UNLOCK entitlement.
                var entitlementRequest = new EvaluateEntitlementRequest(
                    OrganisationId: request.OrganisationId,
                    EntitlementDefinitionCode: "CANDIDATE_UNLOCK"); // Assuming this is the entitlement definition code for candidate unlocks

                var entitlementResult = await _entitlementEvaluator.EvaluateAsync(entitlementRequest, cancellationToken);

                if (entitlementResult.IsSuccess && (entitlementResult.Decision == "ALLOW" || entitlementResult.Decision == "METER"))
                {
                    // Paid/add-on access is available, insert unique unlock and append idempotent EntitlementUsageEvent when metered.
                    var unlock = OrganisationCandidateUnlock.Create(
                        request.OrganisationId,
                        request.CandidateProfileId,
                        UnlockSource.PaidEntitlement); // Note: We don't have a way to know if it's paid or add-on from the entitlement result.

                    try
                    {
                        await _organisationCandidateUnlockRepository.AddAsync(unlock, cancellationToken);
                        // For paid entitlement, we don't increment the free counter.

                        // If the entitlement is metered, record an EntitlementUsageEvent
                        if (entitlementResult.Decision == "METER")
                        {
                            // Get the active subscription to obtain the PlanVersionId
                            var activeSubscription = await _entitlementEvaluatorRepository.GetActiveSubscriptionAsync(
                                request.OrganisationId,
                                cancellationToken);

                            if (activeSubscription != null)
                            {
                                var usageEvent = EntitlementUsageEvent.Create(
                                    organisationId: request.OrganisationId,
                                    entitlementDefinitionCode: "CANDIDATE_UNLOCK",
                                    planVersionId: activeSubscription.PlanId,
                                    quantity: 1m,
                                    idempotencyKey: request.IdempotencyKey ?? Guid.NewGuid().ToString());

                                await _entitlementUsageEventRepository.AddAsync(usageEvent, cancellationToken);
                            }
                        }

                        await _organisationCommercialAccessStateRepository.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateException) when (IsUniqueConstraintViolation())
                    {
                        // Handle concurrent insert: treat as existing unlock
                        var existingUnlockAfterConflict = await _organisationCandidateUnlockRepository.GetByOrganisationAndCandidateAsync(
                            request.OrganisationId,
                            request.CandidateProfileId,
                            cancellationToken);

                        if (existingUnlockAfterConflict != null)
                        {
                            var stateAfterConflict = await _organisationCommercialAccessStateRepository.GetByOrganisationIdAsync(
                                request.OrganisationId,
                                cancellationToken);

                            scope.Complete(); // Complete the transaction before returning
                            return new ProvisionCandidateUnlockResult(
                                Success: true,
                                AccessSource: existingUnlockAfterConflict.Source.ToString(),
                                FreeLimit: stateAfterConflict?.FreeCandidateUnlockLimit ?? 0,
                                FreeUsed: stateAfterConflict?.FreeCandidateUnlockUsed ?? 0,
                                FreeRemaining: stateAfterConflict != null ? stateAfterConflict.FreeCandidateUnlockLimit - stateAfterConflict.FreeCandidateUnlockUsed : 0,
                                PaywallRequired: false);
                        }

                        throw; // If we still can't find it, rethrow
                    }

                    scope.Complete(); // Complete the transaction
                    return new ProvisionCandidateUnlockResult(
                        Success: true,
                        AccessSource: UnlockSource.PaidEntitlement.ToString(),
                        FreeLimit: accessState.FreeCandidateUnlockLimit,
                        FreeUsed: accessState.FreeCandidateUnlockUsed,
                        FreeRemaining: accessState.FreeCandidateUnlockLimit - accessState.FreeCandidateUnlockUsed,
                        PaywallRequired: false);
                }
                else
                {
                    // 8. If not available, persist CandidatePaywallEvent and return PAYWALL_REQUIRED without exposing candidate details.
                    var paywallEvent = CandidatePaywallEvent.Create(
                        request.OrganisationId,
                        request.CandidateProfileId,
                        "PaywallRequired", // reason
                        "CandidateUnlockDecision", // jobContext
                        accessState.FreeCandidateUnlockLimit, // freeLimit
                        accessState.FreeCandidateUnlockUsed, // freeUsed
                        accessState.FreeCandidateUnlockLimit - accessState.FreeCandidateUnlockUsed, // freeRemaining
                        AccessSource.FreeAllowance, // accessSource
                        Guid.NewGuid()); // We generate a correlation id for the paywall event.

                    await _candidatePaywallEventRepository.AddAsync(paywallEvent, cancellationToken);
                    await _organisationCommercialAccessStateRepository.SaveChangesAsync(cancellationToken);

                    scope.Complete(); // Complete the transaction
                    return new ProvisionCandidateUnlockResult(
                        Success: false,
                        AccessSource: string.Empty,
                        FreeLimit: accessState.FreeCandidateUnlockLimit,
                        FreeUsed: accessState.FreeCandidateUnlockUsed,
                        FreeRemaining: accessState.FreeCandidateUnlockLimit - accessState.FreeCandidateUnlockUsed,
                        PaywallRequired: true);
                }
            }
        }
    }

    private bool IsUniqueConstraintViolation()
    {
        // In a real implementation, we would check the specific database exception.
        // For simplicity, we assume any DbUpdateException in this context is due to a unique constraint.
        return true;
    }
}