using System.Text.RegularExpressions;
using IndiaWorks.Domain.E02;
using Microsoft.Extensions.Logging;

namespace IndiaWorks.Application.Features.E02.Implement;

/// <summary>
/// Service responsible for creating an employer Organisation and establishing
/// the creator as the initial OWNER member within a single transaction.
/// Normalizes legal and trade names.
/// </summary>
public sealed class OrganisationCreationService(
    IOrganisationRepository organisationRepository,
    IOrganisationMembershipRepository membershipRepository,
    ILogger<OrganisationCreationService> logger)
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public async Task<CreateOrganisationResult> CreateAsync(
        CreateOrganisationRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validation
        if (request.CreatorUserAccountId == Guid.Empty)
        {
            logger.LogWarning("Organisation creation rejected: CreatorUserAccountId is empty.");
            return CreateOrganisationResult.Failure("VALIDATION_FAILED", "CreatorUserAccountId must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.LegalName))
        {
            logger.LogWarning("Organisation creation rejected: LegalName is empty.");
            return CreateOrganisationResult.Failure("VALIDATION_FAILED", "LegalName must not be empty.");
        }

        // 2. Normalization
        var normalizedLegalName = NormalizeName(request.LegalName);
        var normalizedTradeName = string.IsNullOrWhiteSpace(request.TradeName)
            ? null
            : NormalizeName(request.TradeName);

        logger.LogInformation(
            "Creating organisation with LegalName '{LegalName}' by user {UserId}",
            normalizedLegalName,
            request.CreatorUserAccountId);

        // 3. Domain Entity Creation
        var organisation = Organisation.Create(
            legalName: normalizedLegalName,
            tradeName: normalizedTradeName,
            sector: request.Sector,
            employeeBand: request.EmployeeBand,
            organisationType: request.OrganisationType,
            nic2025Code: request.Nic2025Code);

        var membership = OrganisationMembership.Create(
            organisationId: organisation.Id,
            userAccountId: request.CreatorUserAccountId,
            role: OrganisationRole.Owner);

        // 4. Persistence
        await organisationRepository.AddAsync(organisation, cancellationToken);
        await membershipRepository.AddAsync(membership, cancellationToken);

        // EF-backed repositories share the scoped AppDbContext; one SaveChanges call
        // is the transaction boundary for organisation + OWNER membership creation.
        await organisationRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organisation {OrganisationId} created successfully with OWNER member {UserId}",
            organisation.Id,
            request.CreatorUserAccountId);

        return CreateOrganisationResult.Success(organisation, membership);
    }

    private static string NormalizeName(string name) =>
        WhitespaceRegex.Replace(name.Trim(), " ");
}
