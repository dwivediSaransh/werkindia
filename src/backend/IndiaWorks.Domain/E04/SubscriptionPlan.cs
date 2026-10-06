namespace IndiaWorks.Domain.E04;

/// <summary>
/// Aggregate root representing a subscription plan catalog entry.
/// Each plan has a stable code and multiple immutable versions.
/// </summary>
public sealed class SubscriptionPlan
{
    private readonly List<SubscriptionPlanVersion> _versions = new();

    public Guid Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<SubscriptionPlanVersion> Versions => _versions.AsReadOnly();

    private SubscriptionPlan() { }

    public static SubscriptionPlan Create(string code, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }

    public SubscriptionPlanVersion AddVersion(BillingInterval interval, string currency, decimal? amount = null, int? trialDays = null)
    {
        var version = SubscriptionPlanVersion.Create(Id, interval, currency, amount, trialDays);
        _versions.Add(version);
        return version;
    }
}