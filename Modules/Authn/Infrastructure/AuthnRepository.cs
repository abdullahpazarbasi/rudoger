using Microsoft.EntityFrameworkCore;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Authn.Application;
using Rudoger.Modules.Authn.Domain;

namespace Rudoger.Modules.Authn.Infrastructure;

public sealed class AuthnRepository(AuthnDbContext dbContext, EventStore<AuthnDbContext> eventStore)
    : IAuthnRepository
{
    private const string AggregateType = "user";

    public async Task<UserAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        IReadOnlyList<IDomainEvent> events = await eventStore.LoadAsync(id, cancellationToken);
        if (events.Count == 0)
        {
            return null;
        }

        var aggregate = new UserAggregate();
        aggregate.LoadFromHistory(events);
        return aggregate;
    }

    public Task SaveAsync(UserAggregate aggregate, CancellationToken cancellationToken)
    {
        return eventStore.AppendAsync(
            AggregateType,
            aggregate,
            (domainEvent, token) => ProjectAsync(aggregate.Id, domainEvent, token),
            cancellationToken);
    }

    public async Task<UserCredential?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        UserReadEntity? entity = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Username == username, cancellationToken);
        return entity is null ? null : new UserCredential(entity.Id, entity.Username, entity.PasswordHash);
    }

    private Task ProjectAsync(Guid streamId, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (domainEvent is not UserRegistered registered || registered.UserId != streamId)
        {
            throw new InvalidOperationException($"Unsupported authn event '{domainEvent.GetType().Name}'.");
        }

        dbContext.Users.Add(new UserReadEntity
        {
            Id = registered.UserId,
            Username = registered.Username,
            PasswordHash = registered.PasswordHash,
        });
        return Task.CompletedTask;
    }
}
