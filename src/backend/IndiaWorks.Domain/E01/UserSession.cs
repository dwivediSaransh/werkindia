namespace IndiaWorks.Domain.E01;

/// <summary>
/// Represents an active session for a user on a specific device.
/// Stores the active persona (candidate/employer), expiry, and revocation status.
/// </summary>
public sealed class UserSession
{
    public enum Persona
    {
        Candidate = 1,
        Employer = 2
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid DeviceId { get; private set; }
    public Persona ActivePersona { get; private set; }
    public DateTime ExpiresOn { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime LastActivityOn { get; private set; }
    public bool IsRevoked { get; private set; }

    private UserSession()
    {
    }

    public static UserSession Create(
        Guid userId,
        Guid deviceId,
        Persona activePersona,
        DateTime expiresOn)
    {
        if (expiresOn <= DateTime.UtcNow)
        {
            throw new ArgumentException("Session expiry must be in the future.", nameof(expiresOn));
        }

        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceId = deviceId,
            ActivePersona = activePersona,
            ExpiresOn = expiresOn,
            CreatedOn = DateTime.UtcNow,
            LastActivityOn = DateTime.UtcNow,
            IsRevoked = false
        };
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresOn;

    public void Touch()
    {
        LastActivityOn = DateTime.UtcNow;
    }

    public void Revoke()
    {
        IsRevoked = true;
    }

    public void ExtendExpiry(DateTime newExpiresOn)
    {
        if (newExpiresOn <= DateTime.UtcNow)
        {
            throw new ArgumentException("Session expiry must be in the future.", nameof(newExpiresOn));
        }
        ExpiresOn = newExpiresOn;
    }

    public void SwitchPersona(Persona newPersona)
    {
        ActivePersona = newPersona;
    }
}
