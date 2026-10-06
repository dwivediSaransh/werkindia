namespace IndiaWorks.Application.Tests.Features.E01;

using IndiaWorks.Application.Features.E01;
using IndiaWorks.Domain.E01;

public sealed class CreateUserAccountCommandHandlerTests
{
    [Fact]
    public void Handle_returns_account_and_raises_event()
    {
        var command = CreateUserAccountCommand.Create(
            "Test User",
            "IndiaWorks.Phone",
            "+919876543210");

        var (account, evt) = CreateUserAccountHandler.Handle(command);

        Assert.Equal(command.DisplayName, account.DisplayName);
        Assert.Equal(command.Provider, account.Identifiers.First().Provider);
        Assert.Equal(command.NormalizedIdentifier, account.Identifiers.First().Identifier);
        Assert.NotEqual(Guid.Empty, evt.AccountId);
        Assert.Equal(command.DisplayName, evt.DisplayName);
        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Handle_throws_for_blank_display_name(string displayName)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CreateUserAccountCommand.Create(displayName, "IndiaWorks.Phone", "+919****3210"));
        Assert.Equal("displayName", ex.ParamName);
    }

    [Fact]
    public void Handle_throws_for_blank_provider()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            CreateUserAccountCommand.Create("Test User", null!, "+919****3210"));
        Assert.Equal("provider", ex.ParamName);
    }

    [Fact]
    public void Handle_throws_for_blank_identifier()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            CreateUserAccountCommand.Create("Test User", "IndiaWorks.Phone", null!));
        Assert.Equal("normalizedIdentifier", ex.ParamName);
    }

    [Fact]
    public void Handle_cannot_add_second_verified_login_for_same_provider_on_one_account()
    {
        var command = CreateUserAccountCommand.Create(
            "Test User",
            "IndiaWorks.Phone",
            "+919****3210");

        var (account, _) = CreateUserAccountHandler.Handle(command);
        var phone = account.Identifiers.First();
        account.VerifyLoginIdentifier(phone.Id);

        // Adding a second verified login for the same provider on the SAME account
        // should fail because each account can only have one verified login per provider.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            account.AddLoginIdentifier("IndiaWorks.Phone", "+919****3211"));

        Assert.Contains("verified login identifier", ex.Message);
        Assert.Contains("IndiaWorks.Phone", ex.Message);
    }
}
