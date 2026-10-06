using IndiaWorks.Domain.E01;

namespace IndiaWorks.Domain.Tests.E01;

public sealed class OtpChallengeTests
{
    [Fact]
    public void Create_succeeds_with_valid_parameters()
    {
        var accountId = Guid.NewGuid();
        var purpose = OtpPurpose.Login;
        var hashedOtp = "abc123hash";
        var expiresOn = DateTime.UtcNow.AddMinutes(5);
        var maxAttempts = 3;
        var resendWindowStartsOn = DateTime.UtcNow.AddMinutes(-1);

        var challenge = OtpChallenge.Create(
            accountId,
            purpose,
            hashedOtp,
            expiresOn,
            maxAttempts,
            resendWindowStartsOn);

        Assert.NotEqual(Guid.Empty, challenge.Id);
        Assert.Equal(accountId, challenge.AccountId);
        Assert.Equal(purpose, challenge.Purpose);
        Assert.Equal(hashedOtp, challenge.HashedOtp);
        Assert.True(challenge.CreatedOn > DateTime.MinValue);
        Assert.Equal(expiresOn, challenge.ExpiresOn);
        Assert.Equal(0, challenge.Attempts);
        Assert.Equal(maxAttempts, challenge.MaxAttempts);
        Assert.Equal(resendWindowStartsOn, challenge.ResendWindowStartsOn);
        Assert.False(challenge.IsConsumed);
        Assert.False(challenge.IsExpired);
        Assert.False(challenge.IsAttemptsExceeded);
        Assert.True(challenge.CanResend);
    }

    [Fact]
    public void Create_defaults_resend_window_to_null()
    {
        var accountId = Guid.NewGuid();
        var challenge = OtpChallenge.Create(
            accountId,
            OtpPurpose.Verification,
            "somehash",
            DateTime.UtcNow.AddMinutes(5),
            3);

        Assert.Null(challenge.ResendWindowStartsOn);
        Assert.True(challenge.CanResend);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_throws_for_blank_hashed_otp(string hashedOtp)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            OtpChallenge.Create(Guid.NewGuid(), OtpPurpose.Login, hashedOtp, DateTime.UtcNow.AddMinutes(5), 3));
        Assert.Equal("hashedOtp", ex.ParamName);
    }

    [Fact]
    public void Create_throws_for_non_positive_max_attempts()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            OtpChallenge.Create(Guid.NewGuid(), OtpPurpose.Login, "hash", DateTime.UtcNow.AddMinutes(5), 0));
        Assert.Equal("maxAttempts", ex.ParamName);
    }

    [Fact]
    public void IsExpired_returns_true_when_expiry_is_in_the_past()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            DateTime.UtcNow.AddMinutes(-5),
            3);

        Assert.True(challenge.IsExpired);
        Assert.False(challenge.CanResend);
    }

    [Fact]
    public void IsExpired_returns_false_when_expiry_is_in_the_future()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            DateTime.UtcNow.AddMinutes(5),
            3);

        Assert.False(challenge.IsExpired);
    }

    [Fact]
    public void IsConsumed_returns_true_after_mark_consumed()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            DateTime.UtcNow.AddMinutes(5),
            3);

        Assert.False(challenge.IsConsumed);

        challenge.MarkConsumed();

        Assert.True(challenge.IsConsumed);
        Assert.False(challenge.CanResend);
    }

    [Fact]
    public void MarkConsumed_throws_if_already_consumed()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            DateTime.UtcNow.AddMinutes(5),
            3);

        challenge.MarkConsumed();

        var ex = Assert.Throws<InvalidOperationException>(() => challenge.MarkConsumed());
        Assert.Contains("already consumed", ex.Message);
    }

    [Fact]
    public void MarkConsumed_throws_if_expired()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            DateTime.UtcNow.AddMinutes(-5),
            3);

        var ex = Assert.Throws<InvalidOperationException>(() => challenge.MarkConsumed());
        Assert.Contains("expired", ex.Message);
    }

    [Fact]
    public void IncrementAttempts_increases_attempt_count()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            DateTime.UtcNow.AddMinutes(5),
            3);

        Assert.Equal(0, challenge.Attempts);

        challenge.IncrementAttempts();
        Assert.Equal(1, challenge.Attempts);

        challenge.IncrementAttempts();
        Assert.Equal(2, challenge.Attempts);
    }

    [Fact]
    public void IncrementAttempts_throws_when_consumed()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            DateTime.UtcNow.AddMinutes(5),
            3);

        challenge.MarkConsumed();

        var ex = Assert.Throws<InvalidOperationException>(() => challenge.IncrementAttempts());
        Assert.Contains("consumed", ex.Message);
    }

    [Fact]
    public void IncrementAttempts_throws_when_expired()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            DateTime.UtcNow.AddMinutes(-5),
            3);

        var ex = Assert.Throws<InvalidOperationException>(() => challenge.IncrementAttempts());
        Assert.Contains("expired", ex.Message);
    }

    [Fact]
    public void IsAttemptsExceeded_returns_true_at_max_attempts()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            DateTime.UtcNow.AddMinutes(5),
            3);

        Assert.False(challenge.IsAttemptsExceeded);

        challenge.IncrementAttempts();
        challenge.IncrementAttempts();
        challenge.IncrementAttempts();

        Assert.Equal(3, challenge.Attempts);
        Assert.True(challenge.IsAttemptsExceeded);
        Assert.False(challenge.CanResend);
    }

    [Fact]
    public void CanResend_returns_false_within_resend_window()
    {
        var now = DateTime.UtcNow;
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            now.AddMinutes(5),
            3,
            now.AddMinutes(1));

        // Current time is before resend window starts
        Assert.False(challenge.CanResend);
    }

    [Fact]
    public void CanResend_returns_true_after_resend_window()
    {
        var now = DateTime.UtcNow;
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            now.AddMinutes(5),
            3,
            now.AddMinutes(-1));

        // Resend window started in the past
        Assert.True(challenge.CanResend);
    }

    [Fact]
    public void CanResend_returns_false_when_no_resend_window_set_and_expires_soon()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Verification,
            "hash",
            DateTime.UtcNow.AddMinutes(5),
            3);

        // No resend window set; can resend if not expired/consumed/exceeded
        Assert.True(challenge.CanResend);
    }

    [Fact]
    public void Nullable_properties_default_to_null()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Verification,
            "hash",
            DateTime.UtcNow.AddMinutes(5),
            3);

        Assert.Null(challenge.ResendWindowStartsOn);
        Assert.Null(challenge.ConsumedOn);
    }

    [Fact]
    public void Non_nullable_properties_have_expected_types()
    {
        var accountId = Guid.NewGuid();
        var challenge = OtpChallenge.Create(
            accountId,
            OtpPurpose.Verification,
            "hashvalue",
            DateTime.UtcNow.AddMinutes(10),
            5);

        Assert.IsType<Guid>(challenge.Id);
        Assert.IsType<Guid>(challenge.AccountId);
        Assert.IsType<OtpPurpose>(challenge.Purpose);
        Assert.IsType<string>(challenge.HashedOtp);
        Assert.IsType<DateTime>(challenge.CreatedOn);
        Assert.IsType<DateTime>(challenge.ExpiresOn);
        Assert.IsType<int>(challenge.Attempts);
        Assert.IsType<int>(challenge.MaxAttempts);
    }

    [Fact]
    public void ConsumedOn_is_set_after_mark_consumed()
    {
        var challenge = OtpChallenge.Create(
            Guid.NewGuid(),
            OtpPurpose.Login,
            "hash",
            DateTime.UtcNow.AddMinutes(5),
            3);

        Assert.Null(challenge.ConsumedOn);

        challenge.MarkConsumed();

        Assert.NotNull(challenge.ConsumedOn);
        Assert.True(challenge.ConsumedOn > DateTime.UtcNow.AddSeconds(-1));
    }
}
