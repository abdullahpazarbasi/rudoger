using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.BuildingBlocks.Infrastructure;

public sealed class EventStore<TDbContext>(
    TDbContext dbContext,
    EventTypeRegistry eventTypes,
    ICorrelationContextAccessor correlationContextAccessor,
    TimeProvider timeProvider)
    where TDbContext : EventSourcedDbContext
{
    public async Task<IReadOnlyList<IDomainEvent>> LoadAsync(Guid streamId, CancellationToken cancellationToken)
    {
        List<EventEntity> entities = await dbContext.Events
            .AsNoTracking()
            .Where(item => item.StreamId == streamId)
            .OrderBy(item => item.Version)
            .ToListAsync(cancellationToken);

        return entities
            .Select(item => eventTypes.Deserialize(item.EventType, item.SchemaVersion, item.Payload))
            .ToArray();
    }

    public async Task AppendAsync(
        string aggregateType,
        AggregateRoot aggregate,
        Func<IDomainEvent, CancellationToken, Task> project,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(project);

        if (aggregate.UncommittedEvents.Count == 0)
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        CorrelationContext context = correlationContextAccessor.Current;
        int version = aggregate.OriginalVersion;

        foreach (IDomainEvent domainEvent in aggregate.UncommittedEvents)
        {
            version++;
            dbContext.Events.Add(new EventEntity
            {
                EventId = Guid.CreateVersion7(),
                StreamId = aggregate.Id,
                AggregateType = aggregateType,
                Version = version,
                EventType = eventTypes.NameOf(domainEvent),
                SchemaVersion = 1,
                Payload = eventTypes.Serialize(domainEvent),
                Metadata = JsonSerializer.Serialize(
                    new EventMetadata(context.CorrelationId, context.CausationId, context.UserId)),
                OccurredAtUtc = timeProvider.GetUtcNow(),
            });

            await project(domainEvent, cancellationToken);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            aggregate.MarkChangesAsCommitted();
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConcurrencyException(aggregate.Id, exception);
        }
    }
}
