using IndiaWorks.Domain.E01;

namespace IndiaWorks.Domain.E01;

/// <summary>
/// Represents a hashed OTP challenge for an account.
/// The plain-text OTP is never stored; only the hash is persisted.
/// </summary>
public sealed class OtpChallenge
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public OtpPurpose Purpose { get; private set; }
    public string HashedOtp { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public DateTime ExpiresOn { get; private set; }
    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTime? ResendWindowStartsOn { get; private set; }
    public DateTime? ConsumedOn { get; private set; }

    private OtpChallenge()
    {
    }

    public static OtpChallenge Create(
        Guid accountId,
        OtpPurpose purpose,
        string hashedOtp,
        DateTime expiresOn,
        int maxAttempts,
        DateTime? resendWindowStartsOn = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedOtp);

        return new OtpChallenge
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Purpose = purpose,
            HashedOtp = hashedOtp,
            CreatedOn = DateTime.UtcNow,
            ExpiresOn = expiresOn,
            Attempts = 0,
            MaxAttempts = maxAttempts > 0 ? maxAttempts : throw new ArgumentException("Max attempts must be positive.", nameof(maxAttempts)),
            ResendWindowStartsOn = resendWindowStartsOn,
            ConsumedOn = null
        };
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresOn;

    public bool IsConsumed => ConsumedOn.HasValue;

    public bool IsAttemptsExceeded => Attempts >= MaxAttempts;

    public bool CanResend
    {
        get
        {
            if (IsConsumed || IsExpired || IsAttemptsExceeded)
            {
                return false;
            }

            if (ResendWindowStartsOn.HasValue && DateTime.UtcNow < ResendWindowStartsOn.Value)
            {
                return false;
            }

            return true;
        }
    }

    public void IncrementAttempts()
    {
        if (IsConsumed)
        {
            throw new InvalidOperationException("Cannot increment attempts on a consumed challenge.");
        }

        if (IsExpired)
        {
            throw new InvalidOperationException("Cannot increment attempts on an expired challenge.");
        }

        Attempts++;
    }

    public void MarkConsumed()
    {
        if (IsConsumed)
        {
            throw new InvalidOperationException("Challenge is already consumed.");
        }

        if (IsExpired)
        {
            throw new InvalidOperationException("Cannot consume an expired challenge.");
        }

        ConsumedOn = DateTime.UtcNow;
    }
}
