namespace IndiaWorks.Domain.E01;

/// <summary>
/// Represents a verified login identifier for an account, for example OTP, password or
/// future external-login providers. Provider is a stable FQCN; identifier is normalized
/// and can be verified.
/// </summary>
public sealed class LoginIdentifier
{
    private bool _verified;
    private DateTime? _verifiedOn;

    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public string Provider { get; private set; } = default!;
    public string Identifier { get; private set; } = default!;
    public bool Verified => _verified;
    public DateTime CreatedOn { get; private set; }
    public DateTime? VerifiedOn => _verifiedOn;

    private LoginIdentifier()
    {
    }

    public static LoginIdentifier Create(
        Guid accountId,
        string provider,
        string normalizedIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedIdentifier);

        return new LoginIdentifier
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Provider = provider,
            Identifier = normalizedIdentifier,
            CreatedOn = DateTime.UtcNow
        };
    }

    public void MarkVerified()
    {
        if (_verified)
        {
            throw new InvalidOperationException("Login identifier is already verified.");
        }

        _verified = true;
        _verifiedOn = DateTime.UtcNow;
    }
}
