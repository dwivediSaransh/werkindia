using IndiaWorks.Domain.E02;

namespace IndiaWorks.Application.Features.E02.Implement;

public sealed record CreateOrganisationResult(
    bool IsSuccess,
    Guid? OrganisationId,
    string? LegalName,
    string? TradeName,
    EmployerSector? Sector,
    string? EmployeeBand,
    VerificationStatus? VerificationStatus,
    OrganisationRole? Role,
    DateTime? CreatedAt,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static CreateOrganisationResult Success(Organisation org, OrganisationMembership membership) =>
        new(
            IsSuccess: true,
            OrganisationId: org.Id,
            LegalName: org.LegalName,
            TradeName: org.TradeName,
            Sector: org.Sector,
            EmployeeBand: org.EmployeeBand,
            VerificationStatus: org.VerificationStatus,
            Role: membership.Role,
            CreatedAt: org.CreatedAt);

    public static CreateOrganisationResult Failure(string errorCode, string errorMessage) =>
        new(
            IsSuccess: false,
            OrganisationId: null,
            LegalName: null,
            TradeName: null,
            Sector: null,
            EmployeeBand: null,
            VerificationStatus: null,
            Role: null,
            CreatedAt: null,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
}
