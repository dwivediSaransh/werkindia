using IndiaWorks.Domain.E01;

namespace IndiaWorks.Domain.Tests.E01;

public sealed class UserSessionTests
{
    [Fact]
    public void Create_succeeds_with_valid_properties()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var expiresOn = DateTime.UtcNow.AddHours(1);

        var session = UserSession.Create(userId, deviceId, UserSession.Persona.Candidate, expiresOn);

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(deviceId, session.DeviceId);
        Assert.Equal(UserSession.Persona.Candidate, session.ActivePersona);
        Assert.Equal(expiresOn, session.ExpiresOn);
        Assert.False(session.IsRevoked);
        Assert.True(session.CreatedOn > default(DateTime));
        Assert.True(session.LastActivityOn > default(DateTime));
        Assert.False(session.IsExpired);
    }

    [Fact]
    public void Create_throws_when_expiry_is_in_the_past()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var expiresOn = DateTime.UtcNow.AddMinutes(-5);

        var ex = Assert.Throws<ArgumentException>(() => UserSession.Create(userId, deviceId, UserSession.Persona.Candidate, expiresOn));
        Assert.Equal("expiresOn", ex.ParamName);
    }

    [Fact]
    public void Create_throws_when_expiry_is_equal_to_now()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var expiresOn = DateTime.UtcNow;

        var ex = Assert.Throws<ArgumentException>(() => UserSession.Create(userId, deviceId, UserSession.Persona.Candidate, expiresOn));
        Assert.Equal("expiresOn", ex.ParamName);
    }

    [Fact]
    public void IsExpired_returns_false_for_fresh_session()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var session = UserSession.Create(userId, deviceId, UserSession.Persona.Candidate, DateTime.UtcNow.AddHours(1));

        Assert.False(session.IsExpired);
    }

    [Fact]
    public void IsExpired_checks_current_time_against_expires_on()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var session = UserSession.Create(userId, deviceId, UserSession.Persona.Candidate, DateTime.UtcNow.AddMinutes(30));

        // Session is fresh, should not be expired
        Assert.False(session.IsExpired);

        // Verify the expiry is correctly set (within a small tolerance)
        Assert.True(Math.Abs((session.ExpiresOn - DateTime.UtcNow).TotalMinutes - 30) < 1);
    }

    [Fact]
    public void Touch_updates_last_activity_timestamp()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var session = UserSession.Create(userId, deviceId, UserSession.Persona.Candidate, DateTime.UtcNow.AddHours(1));
        var before = session.LastActivityOn;
        Thread.Sleep(10);
        session.Touch();

        Assert.True(session.LastActivityOn > before);
    }

    [Fact]
    public void Revoke_marks_session_as_revoked()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var session = UserSession.Create(userId, deviceId, UserSession.Persona.Candidate, DateTime.UtcNow.AddHours(1));
        Assert.False(session.IsRevoked);

        session.Revoke();

        Assert.True(session.IsRevoked);
    }

    [Fact]
    public void ExtendExpiry_updates_expiry_time()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var session = UserSession.Create(userId, deviceId, UserSession.Persona.Candidate, DateTime.UtcNow.AddHours(1));
        var newExpiry = DateTime.UtcNow.AddHours(2);
        session.ExtendExpiry(newExpiry);

        Assert.Equal(newExpiry, session.ExpiresOn);
    }

    [Fact]
    public void ExtendExpiry_throws_when_new_expiry_is_in_the_past()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var session = UserSession.Create(userId, deviceId, UserSession.Persona.Candidate, DateTime.UtcNow.AddHours(1));
        var newExpiry = DateTime.UtcNow.AddMinutes(-5);

        var ex = Assert.Throws<ArgumentException>(() => session.ExtendExpiry(newExpiry));
        Assert.Equal("newExpiresOn", ex.ParamName);
    }

    [Fact]
    public void SwitchPersona_changes_active_persona()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var session = UserSession.Create(userId, deviceId, UserSession.Persona.Candidate, DateTime.UtcNow.AddHours(1));
        Assert.Equal(UserSession.Persona.Candidate, session.ActivePersona);

        session.SwitchPersona(UserSession.Persona.Employer);

        Assert.Equal(UserSession.Persona.Employer, session.ActivePersona);
    }

    [Fact]
    public void Persona_enum_has_both_candidate_and_employer_values()
    {
        Assert.Equal(1, (int)UserSession.Persona.Candidate);
        Assert.Equal(2, (int)UserSession.Persona.Employer);
    }
}
