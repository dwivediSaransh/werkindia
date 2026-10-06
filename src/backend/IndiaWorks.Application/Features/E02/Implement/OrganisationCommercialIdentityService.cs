using IndiaWorks.Domain.E02;
using Microsoft.Extensions.Logging;

namespace IndiaWorks.Application.Features.E02.Implement;

/// <summary>
/// Service responsible for deriving and persisting the organisation commercial
/// identity fingerprint from verified external identifiers.
///
/// The fingerprint is a deterministic SHA-256 hash computed from the ordered
/// concatenation of verified identifier hashes (sorted by IdentifierType).
/// It is used by downstream stories to detect duplicate legal entities and
/// prevent free-five reset abuse.
/// </summary>
public sealed class OrganisationCommercialIdentityService(
    IOrganisationRepository organisationRepository,
    ILogger<OrganisationCommercialIdentityService> logger)
{
    /// <summary>
    /// Recomputes the commercial identity fingerprint for an organisation
    /// from its currently verified identifiers and persists the result.
    /// </summary>
    /// <param name="organisationId">The organisation to recompute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the new fingerprint state.</returns>
    public async Task<OrganisationCommercialIdentityResult> RecomputeFingerprintAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default)
    {
        if (organisationId == Guid.Empty)
        {
            logger.LogWarning(
                "Organisation commercial identity fingerprint recompute rejected: OrganisationId is empty.");
            return OrganisationCommercialIdentityResult.Failure(
                "VALIDATION_FAILED",
                "OrganisationId must not be empty.");
        }

        var organisation = await organisationRepository.GetByIdAsync(organisationId, cancellationToken);
        if (organisation is null)
        {
            logger.LogWarning(
                "Organisation commercial identity fingerprint recompute failed: organisation {OrganisationId} not found.",
                organisationId);
            return OrganisationCommercialIdentityResult.Failure(
                "ORGANISATION_NOT_FOUND",
                $"Organisation '{organisationId}' was not found.");
        }

        logger.LogDebug(
            "Recomputing commercial identity fingerprint for organisation {OrganisationId}.",
            organisationId);

        organisation.RecomputeCommercialIdentityFingerprint();

        await organisationRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organisation {OrganisationId} commercial identity fingerprint recomputed. FingerprintPresent: {HasFingerprint}.",
            organisationId,
            organisation.CommercialIdentityFingerprint is not null);

        return OrganisationCommercialIdentityResult.Success(
            organisation.Id,
            organisation.CommercialIdentityFingerprint);
    }

    /// <summary>
    /// Reads the current commercial identity fingerprint for an organisation.
    /// </summary>
    public async Task<OrganisationCommercialIdentityResult> GetFingerprintAsync(
        Guid organisationId,
        CancellationToken cancellationToken = default)
    {
        if (organisationId == Guid.Empty)
        {
            return OrganisationCommercialIdentityResult.Failure(
                "VALIDATION_FAILED",
                "OrganisationId must not be empty.");
        }

        var organisation = await organisationRepository.GetByIdAsync(organisationId, cancellationToken);
        if (organisation is null)
        {
            return OrganisationCommercialIdentityResult.Failure(
                "ORGANISATION_NOT_FOUND",
                $"Organisation '{organisationId}' was not found.");
        }

        return OrganisationCommercialIdentityResult.Success(
            organisation.Id,
            organisation.CommercialIdentityFingerprint);
    }
}
