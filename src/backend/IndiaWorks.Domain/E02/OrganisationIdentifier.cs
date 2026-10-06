namespace IndiaWorks.Domain.E02;

/// <summary>
/// Represents a protected external identifier belonging to an Organisation (e.g. PAN, GSTIN, CIN, LLPIN).
/// Plaintext identifier is never stored unencrypted or unmasked.
/// Unique identification is maintained via cryptographic hash.
/// </summary>
public sealed class OrganisationIdentifier
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public IdentifierType IdentifierType { get; private set; }
    public byte[]? IdentifierValueEncrypted { get; private set; }
    public byte[] IdentifierHash { get; private set; } = default!;
    public string? MaskedDisplay { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; }
    public string? VerificationSource { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private OrganisationIdentifier()
    {
    }

    public static OrganisationIdentifier Create(
        Guid organisationId,
        IdentifierType identifierType,
        byte[] identifierHash,
        string? maskedDisplay = null,
        byte[]? encryptedValue = null)
    {
        if (organisationId == Guid.Empty)
        {
            throw new ArgumentException("OrganisationId must not be empty.", nameof(organisationId));
        }

        if (identifierHash == null || identifierHash.Length == 0)
        {
            throw new ArgumentException("IdentifierHash must not be empty.", nameof(identifierHash));
        }

        return new OrganisationIdentifier
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            IdentifierType = identifierType,
            IdentifierHash = identifierHash,
            MaskedDisplay = maskedDisplay,
            IdentifierValueEncrypted = encryptedValue,
            VerificationStatus = VerificationStatus.Unverified,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkVerified(string verificationSource, DateTime? verifiedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationSource);

        VerificationStatus = VerificationStatus.Verified;
        VerificationSource = verificationSource;
        VerifiedAt = verifiedAt ?? DateTime.UtcNow;
    }

    public void MarkRejected(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        VerificationStatus = VerificationStatus.Rejected;
        VerificationSource = reason;
        VerifiedAt = DateTime.UtcNow;
    }
}
