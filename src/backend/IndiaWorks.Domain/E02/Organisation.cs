namespace IndiaWorks.Domain.E02;

/// <summary>
/// Aggregate root for an Employer Organisation in IndiaWorks.
/// Models legal/trade names, sector, employee size band, verification status,
/// and collection of protected external identifiers.
/// </summary>
public sealed class Organisation
{
    private readonly List<OrganisationIdentifier> _identifiers = new();

    public Guid Id { get; private set; }
    public string LegalName { get; private set; } = default!;
    public string? TradeName { get; private set; }
    public string? OrganisationType { get; private set; }
    public EmployerSector? Sector { get; private set; }
    public string? EmployeeBand { get; private set; }
    public string? Nic2025Code { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[]? CommercialIdentityFingerprint { get; private set; }
    public IReadOnlyCollection<OrganisationIdentifier> Identifiers => _identifiers.AsReadOnly();

    private Organisation()
    {
    }

    public static Organisation Create(
        string legalName,
        string? tradeName = null,
        EmployerSector? sector = null,
        string? employeeBand = null,
        string? organisationType = null,
        string? nic2025Code = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);

        return new Organisation
        {
            Id = Guid.NewGuid(),
            LegalName = legalName.Trim(),
            TradeName = string.IsNullOrWhiteSpace(tradeName) ? null : tradeName.Trim(),
            Sector = sector,
            EmployeeBand = string.IsNullOrWhiteSpace(employeeBand) ? null : employeeBand.Trim(),
            OrganisationType = string.IsNullOrWhiteSpace(organisationType) ? null : organisationType.Trim(),
            Nic2025Code = string.IsNullOrWhiteSpace(nic2025Code) ? null : nic2025Code.Trim(),
            VerificationStatus = VerificationStatus.Unverified,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(
        string legalName,
        string? tradeName,
        EmployerSector? sector,
        string? employeeBand,
        string? organisationType = null,
        string? nic2025Code = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);

        LegalName = legalName.Trim();
        TradeName = string.IsNullOrWhiteSpace(tradeName) ? null : tradeName.Trim();
        Sector = sector;
        EmployeeBand = string.IsNullOrWhiteSpace(employeeBand) ? null : employeeBand.Trim();
        OrganisationType = string.IsNullOrWhiteSpace(organisationType) ? null : organisationType.Trim();
        Nic2025Code = string.IsNullOrWhiteSpace(nic2025Code) ? null : nic2025Code.Trim();
    }

    public void UpdateVerificationStatus(VerificationStatus status)
    {
        VerificationStatus = status;
    }

    /// <summary>
    /// Recomputes the commercial identity fingerprint from verified identifiers.
    /// The fingerprint is SHA-256 of the ordered concatenation of verified
    /// identifier hashes, sorted by IdentifierType for determinism.
    /// Returns null when no verified identifiers exist.
    /// </summary>
    public void RecomputeCommercialIdentityFingerprint()
    {
        var verifiedHashes = _identifiers
            .Where(i => i.VerificationStatus == VerificationStatus.Verified)
            .OrderBy(i => i.IdentifierType)
            .Select(i => i.IdentifierHash)
            .ToArray();

        if (verifiedHashes.Length == 0)
        {
            CommercialIdentityFingerprint = null;
            return;
        }

        using var sha = System.Security.Cryptography.SHA256.Create();
        using var stream = new System.IO.MemoryStream();
        foreach (var hash in verifiedHashes)
        {
            stream.Write(hash);
        }
        CommercialIdentityFingerprint = sha.ComputeHash(stream.ToArray());
    }

    public OrganisationIdentifier AddIdentifier(
        IdentifierType identifierType,
        byte[] identifierHash,
        string? maskedDisplay = null,
        byte[]? encryptedValue = null)
    {
        if (identifierHash == null || identifierHash.Length == 0)
        {
            throw new ArgumentException("IdentifierHash must not be empty.", nameof(identifierHash));
        }

        var existing = _identifiers.FirstOrDefault(i =>
            i.IdentifierType == identifierType &&
            i.IdentifierHash.SequenceEqual(identifierHash));

        if (existing is not null)
        {
            throw new InvalidOperationException($"Identifier of type '{identifierType}' with the same hash already exists for this organisation.");
        }

        var identifier = OrganisationIdentifier.Create(
            Id,
            identifierType,
            identifierHash,
            maskedDisplay,
            encryptedValue);

        _identifiers.Add(identifier);
        return identifier;
    }
}
