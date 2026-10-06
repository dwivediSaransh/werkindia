namespace IndiaWorks.Domain.Tests.E01;

using IndiaWorks.Domain.E01;

public sealed class UserAccountTests
{
    [Fact]
    public void Create_succeeds_with_valid_display_name()
    {
        var account = UserAccount.Create("Test User");

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal("Test User", account.DisplayName);
        Assert.NotEqual(default, account.CreatedOn);
        Assert.Empty(account.Identifiers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_throws_for_blank_display_name(string displayName)
    {
        var ex = Assert.Throws<ArgumentException>(() => UserAccount.Create(displayName));
        Assert.Equal("displayName", ex.ParamName);
    }

    [Fact]
    public void AddLoginIdentifier_succeeds_for_new_provider()
    {
        var account = UserAccount.Create("Test User");
        account.AddLoginIdentifier("IndiaWorks.Phone", "+919876543210");

        Assert.Single(account.Identifiers);
        var identifier = account.Identifiers.First();
        Assert.Equal("IndiaWorks.Phone", identifier.Provider);
        Assert.Equal("+919876543210", identifier.Identifier);
        Assert.False(identifier.Verified);
    }

    [Fact]
    public void AddLoginIdentifier_throws_for_blank_provider()
    {
        var account = UserAccount.Create("Test User");
        var ex = Assert.Throws<ArgumentNullException>(() => account.AddLoginIdentifier(null!, "+919876543210"));
        Assert.Equal("provider", ex.ParamName);
    }

    [Fact]
    public void AddLoginIdentifier_throws_for_blank_identifier()
    {
        var account = UserAccount.Create("Test User");
        var ex = Assert.Throws<ArgumentNullException>(() => account.AddLoginIdentifier("IndiaWorks.Phone", null!));
        Assert.Equal("normalizedIdentifier", ex.ParamName);
    }

    [Fact]
    public void Cannot_add_second_verified_login_for_same_provider()
    {
        var account = UserAccount.Create("Test User");
        account.AddLoginIdentifier("IndiaWorks.Phone", "+919876543210");
        var first = account.Identifiers.First();
        account.VerifyLoginIdentifier(first.Id);

        var ex = Assert.Throws<InvalidOperationException>(() => account.AddLoginIdentifier("IndiaWorks.Phone", "+919876543210"));
        Assert.Contains("verified login identifier", ex.Message);
        Assert.Contains("IndiaWorks.Phone", ex.Message);
    }

    [Fact]
    public void Can_add_unverified_login_for_provider_with_verified_login()
    {
        var account = UserAccount.Create("Test User");
        account.AddLoginIdentifier("IndiaWorks.Phone", "+919876543210");
        var first = account.Identifiers.First();
        account.VerifyLoginIdentifier(first.Id);

        // Adding a second unverified identifier for a different provider is allowed.
        account.AddLoginIdentifier("IndiaWorks.Email", "candidate@example.com");

        Assert.Equal(2, account.Identifiers.Count);
        var phone = account.Identifiers.First(i => i.Provider == "IndiaWorks.Phone");
        var email = account.Identifiers.First(i => i.Provider == "IndiaWorks.Email");
        Assert.True(phone.Verified);
        Assert.False(email.Verified);
    }

    [Fact]
    public void VerifyLoginIdentifier_succeeds_for_known_identifier()
    {
        var account = UserAccount.Create("Test User");
        account.AddLoginIdentifier("IndiaWorks.Phone", "+919876543210");
        var identifier = account.Identifiers.First();
        account.VerifyLoginIdentifier(identifier.Id);

        Assert.True(identifier.Verified);
        Assert.True(identifier.VerifiedOn > default(DateTime));
    }

    [Fact]
    public void VerifyLoginIdentifier_throws_for_unknown_identifier()
    {
        var account = UserAccount.Create("Test User");
        var ex = Assert.Throws<ArgumentException>(() => account.VerifyLoginIdentifier(Guid.NewGuid()));
        Assert.Equal("identifierId", ex.ParamName);
    }

    [Fact]
    public void VerifyLoginIdentifier_throws_if_already_verified()
    {
        var account = UserAccount.Create("Test User");
        account.AddLoginIdentifier("IndiaWorks.Phone", "+919876543210");
        var identifier = account.Identifiers.First();
        account.VerifyLoginIdentifier(identifier.Id);

        var ex = Assert.Throws<InvalidOperationException>(() => identifier.MarkVerified());
        Assert.Contains("already verified", ex.Message);
    }

    [Fact]
    public void Identifiers_collection_is_read_only()
    {
        var account = UserAccount.Create("Test User");
        account.AddLoginIdentifier("IndiaWorks.Phone", "+919876543210");

        var collection = account.Identifiers;
        Assert.IsAssignableFrom<IReadOnlyCollection<LoginIdentifier>>(collection);
    }

    [Fact]
    public void CreateLoginIdentifier_throws_for_blank_provider()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => LoginIdentifier.Create(Guid.NewGuid(), null!, "value"));
        Assert.Equal("provider", ex.ParamName);
    }

    [Fact]
    public void CreateLoginIdentifier_throws_for_blank_identifier()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => LoginIdentifier.Create(Guid.NewGuid(), "provider", null!));
        Assert.Equal("normalizedIdentifier", ex.ParamName);
    }

    [Fact]
    public void LoginIdentifier_exposes_created_timestamps()
    {
        var id = Guid.NewGuid();
        var identifier = LoginIdentifier.Create(id, "IndiaWorks.Phone", "+919****3210");

        Assert.Equal(id, identifier.AccountId);
        Assert.True(identifier.CreatedOn > DateTime.MinValue);
    }
}
