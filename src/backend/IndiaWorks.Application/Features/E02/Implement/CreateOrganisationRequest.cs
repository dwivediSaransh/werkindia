using IndiaWorks.Domain.E02;

namespace IndiaWorks.Application.Features.E02.Implement;

public sealed record CreateOrganisationRequest(
    Guid CreatorUserAccountId,
    string LegalName,
    string? TradeName = null,
    EmployerSector? Sector = null,
    string? EmployeeBand = null,
    string? OrganisationType = null,
    string? Nic2025Code = null);
