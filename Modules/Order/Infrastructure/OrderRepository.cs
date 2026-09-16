using Microsoft.EntityFrameworkCore;
using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Order.Application;
using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Infrastructure;

public sealed class OrderRepository(
    OrderDbContext dbContext,
    EventStore<OrderDbContext> eventStore,
    TimeProvider timeProvider) : IOrderRepository
{
    private const string AggregateType = "order";

    private Guid CurrentStreamId { get; set; }

    public async Task<OrderAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        IReadOnlyList<IDomainEvent> events = await eventStore.LoadAsync(id, cancellationToken);
        if (events.Count == 0)
        {
            return null;
        }

        var aggregate = new OrderAggregate();
        aggregate.LoadFromHistory(events);
        return aggregate;
    }

    public Task SaveAsync(OrderAggregate aggregate, CancellationToken cancellationToken)
    {
        CurrentStreamId = aggregate.Id;
        return eventStore.AppendAsync(AggregateType, aggregate, ProjectAsync, cancellationToken);
    }

    public async Task<OrderView?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        OrderReadEntity? entity = await dbContext.Orders.AsNoTracking()
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? null : ToView(entity);
    }

    public async Task<Page<OrderView>> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<OrderReadEntity> query = dbContext.Orders.AsNoTracking().Include(item => item.Lines);
        int totalCount = await query.CountAsync(cancellationToken);
        OrderReadEntity[] items = await query.OrderByDescending(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new Page<OrderView>(items.Select(ToView).ToArray(), pageNumber, pageSize, totalCount);
    }

    public Task<bool> HasAnyOrderAsync(Guid productId, CancellationToken cancellationToken)
    {
        return dbContext.OrderLines.AsNoTracking().AnyAsync(item => item.ProductId == productId, cancellationToken);
    }

    public async Task<OrderTransitionView?> GetTransitionAsync(
        Guid orderId,
        Guid transitionId,
        CancellationToken cancellationToken)
    {
        OrderTransitionReadEntity? entity = await dbContext.OrderTransitions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.OrderId == orderId && item.Id == transitionId, cancellationToken);
        return entity is null ? null : new OrderTransitionView(entity.Id, entity.OrderId, entity.Target, entity.Status);
    }

    private Task ProjectAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return domainEvent switch
        {
            OrderPlaced placed => ProjectPlacedAsync(placed, cancellationToken),
            OrderTransitionRequested requested => ProjectTransitionRequestedAsync(requested, cancellationToken),
            OrderShipped shipped => ProjectTransitionCompletedAsync(shipped.TransitionId, OrderStatus.Shipped, cancellationToken),
            OrderCancelled cancelled => ProjectTransitionCompletedAsync(cancelled.TransitionId, OrderStatus.Cancelled, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported order event '{domainEvent.GetType().Name}'."),
        };
    }

    private Task ProjectPlacedAsync(OrderPlaced placed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Orders.Add(new OrderReadEntity
        {
            Id = placed.OrderId,
            OrderNumber = placed.OrderNumber,
            Status = OrderStatus.Placed,
            UserId = placed.UserId,
            Lines = placed.Lines.Select(line => ToEntity(placed.OrderId, line)).ToList(),
        });
        return Task.CompletedTask;
    }

    private async Task ProjectTransitionRequestedAsync(
        OrderTransitionRequested requested,
        CancellationToken cancellationToken)
    {
        OrderReadEntity order = await dbContext.Orders.SingleAsync(item => item.Id == CurrentStreamId, cancellationToken);
        order.PendingTransitionId = requested.TransitionId;
        order.PendingTransitionTarget = requested.Target;
        dbContext.OrderTransitions.Add(new OrderTransitionReadEntity
        {
            Id = requested.TransitionId,
            OrderId = CurrentStreamId,
            Target = requested.Target,
            Status = OrderTransitionStatus.Pending,
        });
        AddOutbox(OrderWorkflowMessageType.ProcessTransition, CurrentStreamId, requested.TransitionId);
    }

    private async Task ProjectTransitionCompletedAsync(
        Guid transitionId,
        OrderStatus status,
        CancellationToken cancellationToken)
    {
        OrderReadEntity order = await dbContext.Orders.SingleAsync(item => item.Id == CurrentStreamId, cancellationToken);
        OrderTransitionReadEntity transition = await dbContext.OrderTransitions.SingleAsync(
            item => item.Id == transitionId,
            cancellationToken);
        order.Status = status;
        order.PendingTransitionId = null;
        order.PendingTransitionTarget = null;
        transition.Status = OrderTransitionStatus.Succeeded;
    }

    private void AddOutbox(OrderWorkflowMessageType type, Guid aggregateId, Guid? secondaryId)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        dbContext.OutboxMessages.Add(new OrderOutboxMessageEntity
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            AggregateId = aggregateId,
            SecondaryId = secondaryId,
            OccurredAtUtc = now,
            NextAttemptAtUtc = now,
        });
    }

    private static OrderLineReadEntity ToEntity(Guid orderId, OrderLineDefinition line)
    {
        return new OrderLineReadEntity
        {
            Id = line.Id,
            OrderId = orderId,
            Num = line.Num,
            ProductId = line.ProductId,
            UomCode = line.UomCode,
            Quantity = line.Quantity,
            UnitPriceAmount = line.UnitPriceAmount,
            UnitPriceCurrencyCode = line.UnitPriceCurrencyCode,
            BaseQuantity = line.BaseQuantity,
        };
    }

    private static OrderView ToView(OrderReadEntity entity)
    {
        return new OrderView(
            entity.Id,
            entity.OrderNumber,
            entity.Status,
            entity.UserId,
            entity.Lines.OrderBy(item => item.Num).Select(item => new OrderLineView(
                item.Id,
                item.Num,
                item.ProductId,
                item.UomCode,
                item.Quantity,
                item.UnitPriceAmount,
                item.UnitPriceCurrencyCode)).ToArray(),
            entity.PendingTransitionId,
            entity.PendingTransitionTarget);
    }
}
