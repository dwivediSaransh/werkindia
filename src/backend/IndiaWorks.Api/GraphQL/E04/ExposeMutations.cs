using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using IndiaWorks.Application.Features.E04.Implement;
using IndiaWorks.Application.Features.E04.Provision;
using IndiaWorks.Api.GraphQL.E04.Types;

namespace IndiaWorks.Api.GraphQL.E04;

/// <summary>
/// GraphQL mutations for candidate unlock operations.
/// </summary>
[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class UnlockCandidateProfileMutations
{
    /// <summary>
    /// Unlocks a candidate profile for an organisation member.
    /// </summary>
    /// <param name="input">The input containing organisation, candidate, and caller details.</param>
    /// <param name="unlockDecisionService">Service for making candidate unlock decisions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the unlock candidate profile operation.</returns>
    public async Task<UnlockCandidateProfileResult> UnlockCandidateProfile(
        UnlockCandidateProfileInput input,
        [Service] CandidateUnlockDecisionService unlockDecisionService,
        CancellationToken cancellationToken)
    {
        // Map GraphQL input to service request
        var request = new ProvisionCandidateUnlockRequest(
            OrganisationId: input.OrganisationId,
            CallerUserId: input.CallerUserId,
            CandidateProfileId: input.CandidateProfileId,
            IdempotencyKey: input.IdempotencyKey);

        // Call the service
        var serviceResult = await unlockDecisionService.HandleAsync(request, cancellationToken);

        // Map service result to GraphQL result
        return new UnlockCandidateProfileResult
        {
            Success = serviceResult.Success,
            AccessSource = serviceResult.AccessSource,
            FreeLimit = serviceResult.FreeLimit,
            FreeUsed = serviceResult.FreeUsed,
            FreeRemaining = serviceResult.FreeRemaining,
            PaywallRequired = serviceResult.PaywallRequired,
            ErrorCode = serviceResult.ErrorCode,
            ErrorMessage = serviceResult.ErrorMessage
        };
    }
}