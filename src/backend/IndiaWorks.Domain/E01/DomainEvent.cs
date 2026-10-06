namespace IndiaWorks.Domain.E01;

/// <summary>
/// Base class for domain events raised by the auth/account aggregate.
/// Kept minimal for this story; extended by follow-up auth stories as needed.
/// </summary>
public abstract class DomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Marker interface for domain events.
/// </summary>
public interface IDomainEvent
{
}
