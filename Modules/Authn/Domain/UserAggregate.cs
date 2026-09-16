using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Authn.Domain;

public sealed class UserAggregate : AggregateRoot
{
    public string Username { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public static UserAggregate Register(Guid id, string username, string passwordHash)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("user-id-required", "User id is required.");
        }

        string normalizedUsername = Guard.Required(
            username,
            "username-invalid",
            "Username",
            UserRules.UsernameMaximumLength).ToLowerInvariant();
        string normalizedHash = Guard.Required(
            passwordHash,
            "password-hash-invalid",
            "Password hash",
            UserRules.PasswordHashMaximumLength);
        var aggregate = new UserAggregate();
        aggregate.Raise(new UserRegistered(id, normalizedUsername, normalizedHash));
        return aggregate;
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        if (domainEvent is not UserRegistered registered)
        {
            throw new InvalidOperationException($"Unsupported authn event '{domainEvent.GetType().Name}'.");
        }

        Id = registered.UserId;
        Username = registered.Username;
        PasswordHash = registered.PasswordHash;
    }
}
