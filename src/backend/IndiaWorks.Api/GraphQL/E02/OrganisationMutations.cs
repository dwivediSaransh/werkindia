using HotChocolate;
using HotChocolate.Types;
using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Domain.E02;

namespace IndiaWorks.Api.GraphQL.E02;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class OrganisationMutations
{
    /// <summary>
    /// Create an organisation with the caller as OWNER member.
    /// Validation and business rules are enforced in the application service.
    /// Returns a structured payload rather than throwing on validation failures.
    /// </summary>
    public async Task<CreateOrganisationResult> CreateOrganisation(
        CreateOrganisationInput input,
        [Service] OrganisationCreationService creationService,
        CancellationToken cancellationToken)
    {
        EmployerSector? sector = null;
        if (input.Sector.HasValue)
        {
            sector = input.Sector.Value switch
            {
                EmployerSectorInput.Government => EmployerSector.Government,
                EmployerSectorInput.Psu        => EmployerSector.Psu,
                EmployerSectorInput.Ngo        => EmployerSector.Ngo,
                EmployerSectorInput.Household  => EmployerSector.Household,
                _                              => EmployerSector.Private
            };
        }

        var request = new CreateOrganisationRequest(
            CreatorUserAccountId: input.CreatorUserAccountId,
            LegalName: input.LegalName,
            TradeName: input.TradeName,
            Sector: sector,
            EmployeeBand: input.EmployeeBand,
            OrganisationType: input.OrganisationType,
            Nic2025Code: input.Nic2025Code);

        return await creationService.CreateAsync(request, cancellationToken);
    }
}

/// <summary>
/// GQL enum matching the EmployerSector domain enum.
/// Sent without quotes in GQL (e.g. sector: Private).
/// </summary>
public enum EmployerSectorInput
{
    Private,
    Government,
    Psu,
    Ngo,
    Household
}
