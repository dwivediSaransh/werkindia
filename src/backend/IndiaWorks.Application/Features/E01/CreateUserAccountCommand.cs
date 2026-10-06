namespace IndiaWorks.Application.Features.E01;

using IndiaWorks.Domain.E01;

public sealed record CreateUserAccountCommand(
    string DisplayName,
    string Provider,
    string NormalizedIdentifier)
{
    public static CreateUserAccountCommand Create(string displayName, string provider, string normalizedIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedIdentifier);
        return new CreateUserAccountCommand(displayName, provider, normalizedIdentifier);
    }
}

public sealed class AccountCreatedEvent : DomainEvent, IDomainEvent
{
    public Guid AccountId { get; }
    public string DisplayName { get; }

    public AccountCreatedEvent(Guid accountId, string displayName)
    {
        AccountId = accountId;
        DisplayName = displayName;
    }
}

public static class CreateUserAccountHandler
{
    public static (UserAccount Account, AccountCreatedEvent Event) Handle(CreateUserAccountCommand command)
    {
        var account = UserAccount.Create(command.DisplayName);
        account.AddLoginIdentifier(command.Provider, command.NormalizedIdentifier);

        var evt = new AccountCreatedEvent(account.Id, account.DisplayName);
        return (account, evt);
    }
}
