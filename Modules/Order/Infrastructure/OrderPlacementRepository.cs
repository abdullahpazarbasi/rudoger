using Microsoft.EntityFrameworkCore;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Order.Application;
using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Infrastructure;

public sealed class OrderPlacementRepository(
    OrderDbContext dbContext,
    EventStore<OrderDbContext> eventStore,
    TimeProvider timeProvider) : IOrderPlacementRepository
{
    private const string AggregateType = "order-placement";

    private Guid CurrentStreamId { get; set; }

    public async Task<OrderPlacementAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        IReadOnlyList<IDomainEvent> events = await eventStore.LoadAsync(id, cancellationToken);
        if (events.Count == 0)
        {
            return null;
        }

        var aggregate = new OrderPlacementAggregate();
        aggregate.LoadFromHistory(events);
        return aggregate;
    }

    public Task SaveAsync(OrderPlacementAggregate aggregate, CancellationToken cancellationToken)
    {
        CurrentStreamId = aggregate.Id;
        return eventStore.AppendAsync(AggregateType, aggregate, ProjectAsync, cancellationToken);
    }

    public async Task<OrderPlacementView?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        OrderPlacementReadEntity? entity = await dbContext.OrderPlacements.AsNoTracking()
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? null : ToView(entity);
    }

    public async Task<OrderPlacementView?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        OrderPlacementReadEntity? entity = await dbContext.OrderPlacements.AsNoTracking()
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);
        return entity is null ? null : ToView(entity);
    }

    private Task ProjectAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return domainEvent switch
        {
            OrderPlacementRequested requested => ProjectRequestedAsync(requested, cancellationToken),
            OrderPlacementSucceeded => ProjectSucceededAsync(cancellationToken),
            OrderPlacementFailed failed => ProjectFailedAsync(failed, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported order placement event '{domainEvent.GetType().Name}'."),
        };
    }

    private Task ProjectRequestedAsync(OrderPlacementRequested requested, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.OrderPlacements.Add(new OrderPlacementReadEntity
        {
            Id = requested.PlacementId,
            OrderId = requested.OrderId,
            UserId = requested.UserId,
            IdempotencyKey = requested.IdempotencyKey,
            Status = OrderPlacementStatus.Pending,
            Lines = requested.Lines.Select(line => new OrderPlacementLineReadEntity
            {
                Id = line.Id,
                PlacementId = requested.PlacementId,
                Num = line.Num,
                ProductId = line.ProductId,
                UomCode = line.UomCode,
                Quantity = line.Quantity,
                ReservationSourceEventId = line.ReservationSourceEventId,
                ReleaseSourceEventId = line.ReleaseSourceEventId,
            }).ToList(),
        });
        DateTimeOffset now = timeProvider.GetUtcNow();
        dbContext.OutboxMessages.Add(new OrderOutboxMessageEntity
        {
            Id = Guid.CreateVersion7(),
            Type = OrderWorkflowMessageType.ProcessPlacement,
            AggregateId = requested.PlacementId,
            OccurredAtUtc = now,
            NextAttemptAtUtc = now,
        });
        return Task.CompletedTask;
    }

    private async Task ProjectSucceededAsync(CancellationToken cancellationToken)
    {
        OrderPlacementReadEntity placement = await dbContext.OrderPlacements.SingleAsync(
            item => item.Id == CurrentStreamId,
            cancellationToken);
        placement.Status = OrderPlacementStatus.Succeeded;
    }

    private async Task ProjectFailedAsync(OrderPlacementFailed failed, CancellationToken cancellationToken)
    {
        OrderPlacementReadEntity placement = await dbContext.OrderPlacements.SingleAsync(
            item => item.Id == CurrentStreamId,
            cancellationToken);
        placement.Status = OrderPlacementStatus.Failed;
        placement.FailureCode = failed.FailureCode;
        placement.FailureDetail = failed.FailureDetail;
    }

    private static OrderPlacementView ToView(OrderPlacementReadEntity entity)
    {
        return new OrderPlacementView(
            entity.Id,
            entity.OrderId,
            entity.UserId,
            entity.Status,
            entity.Lines.OrderBy(item => item.Num).Select(line => new OrderPlacementLineView(
                line.Num,
                line.ProductId,
                line.UomCode,
                line.Quantity)).ToArray(),
            entity.FailureCode,
            entity.FailureDetail);
    }
}
