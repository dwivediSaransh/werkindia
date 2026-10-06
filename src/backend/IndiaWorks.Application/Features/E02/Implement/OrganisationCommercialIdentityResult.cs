using IndiaWorks.Domain.E02;

namespace IndiaWorks.Application.Features.E02.Implement;

/// <summary>
/// Result of a commercial identity fingerprint operation.
/// </summary>
public sealed record OrganisationCommercialIdentityResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid OrganisationId { get; init; }
    public byte[]? Fingerprint { get; init; }

    public static OrganisationCommercialIdentityResult Success(Guid organisationId, byte[]? fingerprint)
    {
        return new OrganisationCommercialIdentityResult
        {
            IsSuccess = true,
            OrganisationId = organisationId,
            Fingerprint = fingerprint
        };
    }

    public static OrganisationCommercialIdentityResult Failure(string errorCode, string errorMessage)
    {
        return new OrganisationCommercialIdentityResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
