namespace IndiaWorks.Api.GraphQL.E02;

public sealed record CreateOrganisationInput(
    Guid CreatorUserAccountId,
    string LegalName,
    string? TradeName = null,
    EmployerSectorInput? Sector = null,
    string? EmployeeBand = null,
    string? OrganisationType = null,
    string? Nic2025Code = null);
