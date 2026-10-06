using IndiaWorks.Application.Features.E01.Create;
using IndiaWorks.Domain.E01;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IndiaWorks.Application.Tests.Features.E01;

public sealed class CreateTests
{
    private sealed class NullOtpSenderLogger : ILogger<DevelopmentOtpSender>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private sealed class TestJwtLogger : ILogger<JwtTokenService>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    // OTP sender tests (existing)...

    [Fact]
    public async Task DevelopmentOtpSender_sends_successfully_with_valid_parameters()
    {
        var sender = new DevelopmentOtpSender(new NullOtpSenderLogger());

        var request = new OtpSendRequest(
            AccountId: Guid.NewGuid(),
            Purpose: OtpPurpose.Login,
            NormalizedIdentifier: "+919****3210",
            Channel: OtpChannel.Sms);

        var result = await sender.SendAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.ProviderMessageId);
        Assert.NotEmpty(result.ProviderMessageId);
        Assert.StartsWith("dev-", result.ProviderMessageId);
        Assert.InRange(result.SentAt, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DevelopmentOtpSender_throws_for_blank_identifier(string identifier)
    {
        var sender = new DevelopmentOtpSender(new NullOtpSenderLogger());

        var request = new OtpSendRequest(
            AccountId: Guid.NewGuid(),
            Purpose: OtpPurpose.Verification,
            NormalizedIdentifier: identifier!,
            Channel: OtpChannel.Email);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sender.SendAsync(request));
        Assert.Equal("request.NormalizedIdentifier", ex.ParamName);
    }

    [Theory]
    [InlineData(OtpChannel.Sms)]
    [InlineData(OtpChannel.Email)]
    [InlineData(OtpChannel.Voice)]
    public async Task DevelopmentOtpSender_accepts_valid_channels(OtpChannel channel)
    {
        var sender = new DevelopmentOtpSender(new NullOtpSenderLogger());

        var request = new OtpSendRequest(
            AccountId: Guid.NewGuid(),
            Purpose: OtpPurpose.Login,
            NormalizedIdentifier: "user@example.com",
            Channel: channel);

        var result = await sender.SendAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.ProviderMessageId);
    }

    [Fact]
    public async Task DevelopmentOtpSender_throws_for_invalid_channel()
    {
        var sender = new DevelopmentOtpSender(new NullOtpSenderLogger());

        // Use an invalid enum value by casting an unsupported int.
        var invalidChannel = (OtpChannel)99;

        var request = new OtpSendRequest(
            AccountId: Guid.NewGuid(),
            Purpose: OtpPurpose.Verification,
            NormalizedIdentifier: "test@example.com",
            Channel: invalidChannel);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sender.SendAsync(request));
        Assert.Equal("Channel", ex.ParamName);
    }

    // JWT access-token tests

    [Fact]
    public void JwtTokenService_issues_token_with_sub_session_and_persona_claims()
    {
        var logger = new TestJwtLogger();
        var service = new JwtTokenService(logger, new JwtSettings("IndiaWorks", "IndiaWorks", AccessTokenSeconds: 900));

        var subject = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var persona = "candidate";

        var result = service.CreateAccessToken(new CreateAccessTokenRequest(subject, sessionId, persona));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.AccessToken);
        Assert.StartsWith("eyJ", result.AccessToken);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.AccessToken);

        Assert.Equal(subject.ToString("N"), token.Subject);
        Assert.Contains(token.Claims, c => c.Type == "sid" && c.Value == sessionId.ToString("N"));
        Assert.Contains(token.Claims, c => c.Type == "persona" && c.Value == persona);

        // Short-lived: expiry should be near now + configured lifetime, not far future.
        Assert.True(token.ValidTo > DateTime.UtcNow);
        Assert.True(token.ValidTo <= DateTime.UtcNow.AddMinutes(16)); // 15 minute default ceiling
    }

    // JWT access-token tests

    public static IEnumerable<object[]> InvalidRequestCases()
    {
        yield return new object[] { Guid.Empty, Guid.NewGuid(), "candidate" };
        yield return new object[] { Guid.NewGuid(), Guid.Empty, "candidate" };
        yield return new object[] { Guid.NewGuid(), Guid.NewGuid(), "" };
        yield return new object[] { Guid.NewGuid(), Guid.NewGuid(), "   " };
    }

    [Theory]
    [MemberData(nameof(InvalidRequestCases))]
    public void JwtTokenService_rejects_invalid_request(Guid subject, Guid sessionId, string persona)
    {
        var logger = new TestJwtLogger();
        var service = new JwtTokenService(logger, new JwtSettings("IndiaWorks", "IndiaWorks"));

        var result = service.CreateAccessToken(new CreateAccessTokenRequest(subject, sessionId, persona));

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void JwtTokenService_does_not_log_plaintext_token_or_persona()
    {
        var logger = new TestJwtLogger();
        var service = new JwtTokenService(logger, new JwtSettings("IndiaWorks", "IndiaWorks"));

        var subject = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var persona = "employer";

        _ = service.CreateAccessToken(new CreateAccessTokenRequest(subject, sessionId, persona));

        var allLogText = string.Join(" ", logger.Messages);

        // Token value should not be emitted in full.
        Assert.DoesNotContain("eyJ", allLogText);

        // Persona plaintext should be masked in logs.
        Assert.DoesNotContain(persona, allLogText);
    }
}
