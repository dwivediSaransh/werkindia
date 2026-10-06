namespace IndiaWorks.Domain.E01;

/// <summary>
/// Aggregate root for a single IndiaWorks account that can hold multiple verified
/// login identifiers.
/// </summary>
public sealed class UserAccount
{
    private readonly List<LoginIdentifier> _identifiers = new();

    public Guid Id { get; private set; }
    public string DisplayName { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public IReadOnlyCollection<LoginIdentifier> Identifiers => _identifiers.AsReadOnly();

    private UserAccount()
    {
    }

    public static UserAccount Create(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new UserAccount
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName,
            CreatedOn = DateTime.UtcNow
        };
    }

    public void AddLoginIdentifier(string provider, string normalizedIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedIdentifier);

        var existingVerifiedLoginForProvider = _identifiers
            .Where(i => i.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            .Select(i => i.Verified)
            .FirstOrDefault();

        if (existingVerifiedLoginForProvider)
        {
            throw new InvalidOperationException(
                $"A verified login identifier for provider '{provider}' already exists on this account.");
        }

        _identifiers.Add(LoginIdentifier.Create(Id, provider, normalizedIdentifier));
    }

    public void VerifyLoginIdentifier(Guid identifierId)
    {
        var identifier = _identifiers.FirstOrDefault(i => i.Id == identifierId);
        if (identifier is null)
        {
            throw new ArgumentException("Unknown login identifier.", nameof(identifierId));
        }

        identifier.MarkVerified();
    }
}
