using Microsoft.EntityFrameworkCore;
using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Inventory.Application;
using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Infrastructure;

public sealed class InventoryRepository(
    InventoryDbContext dbContext,
    EventStore<InventoryDbContext> eventStore,
    TimeProvider timeProvider) : IInventoryRepository
{
    private const string AggregateType = "stock-item";

    public async Task<StockItemAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        IReadOnlyList<IDomainEvent> events = await eventStore.LoadAsync(id, cancellationToken);
        if (events.Count == 0)
        {
            return null;
        }

        var aggregate = new StockItemAggregate();
        aggregate.LoadFromHistory(events);
        return aggregate;
    }

    public async Task<StockItemAggregate?> LoadByProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        Guid? id = await dbContext.StockItems.AsNoTracking()
            .Where(item => item.ProductId == productId)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return id.HasValue ? await LoadAsync(id.Value, cancellationToken) : null;
    }

    public Task SaveAsync(StockItemAggregate aggregate, CancellationToken cancellationToken)
    {
        return eventStore.AppendAsync(AggregateType, aggregate, ProjectAsync, cancellationToken);
    }

    public async Task<StockItemView?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        StockItemReadEntity? entity = await dbContext.StockItems.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? null : ToView(entity);
    }

    public async Task<StockItemView?> GetByProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        StockItemReadEntity? entity = await dbContext.StockItems.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProductId == productId, cancellationToken);
        return entity is null ? null : ToView(entity);
    }

    public async Task<StockItemCreationView?> GetByCreationIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        StockItemReadEntity? entity = await dbContext.StockItems.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CreationIdempotencyKey == idempotencyKey, cancellationToken);
        return entity is null
            ? null
            : new StockItemCreationView(
                ToView(entity),
                entity.OpeningUomCode,
                entity.RequestedOpeningQuantity,
                entity.CreationIdempotencyKey);
    }

    public async Task<Page<StockItemView>> ListAsync(
        Guid? productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<StockItemReadEntity> query = dbContext.StockItems.AsNoTracking();
        if (productId.HasValue)
        {
            query = query.Where(item => item.ProductId == productId.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        StockItemReadEntity[] items = await query.OrderBy(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new Page<StockItemView>(items.Select(ToView).ToArray(), pageNumber, pageSize, totalCount);
    }

    public async Task<StockMovementView?> GetMovementByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        StockMovementReadEntity? entity = await dbContext.StockMovements.AsNoTracking()
            .SingleOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);
        return entity is null ? null : ToView(entity);
    }

    public async Task<Page<StockMovementView>> ListMovementsAsync(
        Guid stockItemId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.StockItems.AsNoTracking().AnyAsync(item => item.Id == stockItemId, cancellationToken))
        {
            throw new NotFoundException(
                InventoryFailureCode.StockItemNotFound,
                $"Stock item '{stockItemId}' was not found.");
        }

        IQueryable<StockMovementReadEntity> query = dbContext.StockMovements.AsNoTracking()
            .Where(item => item.StockItemId == stockItemId);
        int totalCount = await query.CountAsync(cancellationToken);
        StockMovementReadEntity[] items = await query
            .OrderByDescending(item => item.OccurredAtUtc)
            .ThenBy(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new Page<StockMovementView>(items.Select(ToView).ToArray(), pageNumber, pageSize, totalCount);
    }

    private Task ProjectAsync(Guid aggregateId, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return domainEvent switch
        {
            StockItemOpened opened => ProjectOpenedAsync(opened, cancellationToken),
            IStockMovementEvent movement => ProjectMovementAsync(aggregateId, movement, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported inventory event '{domainEvent.GetType().Name}'."),
        };
    }

    private Task ProjectOpenedAsync(StockItemOpened opened, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.StockItems.Add(new StockItemReadEntity
        {
            Id = opened.StockItemId,
            ProductId = opened.ProductId,
            BaseUomCode = opened.BaseUomCode,
            OpeningQuantity = opened.OpeningQuantity,
            OpeningUomCode = opened.OpeningUomCode,
            RequestedOpeningQuantity = opened.RequestedOpeningQuantity,
            CreationIdempotencyKey = opened.IdempotencyKey,
        });
        AddReleaseUsageOutbox(opened.ProductId, opened.ProductUsageOperationId);
        return Task.CompletedTask;
    }

    private async Task ProjectMovementAsync(
        Guid aggregateId,
        IStockMovementEvent movementEvent,
        CancellationToken cancellationToken)
    {
        StockMovementData movement = movementEvent.Movement;
        StockMovementType movementType = ToMovementType(movementEvent);
        StockItemReadEntity item = dbContext.StockItems.Local
                .FirstOrDefault(entity => entity.Id == aggregateId)
            ?? await dbContext.StockItems.SingleAsync(entity => entity.Id == aggregateId, cancellationToken);
        item.OnHandQuantity += movement.OnHandQuantityDelta;
        item.ReservedQuantity += movement.ReservedQuantityDelta;
        dbContext.StockMovements.Add(new StockMovementReadEntity
        {
            Id = movement.MovementId,
            StockItemId = aggregateId,
            Type = movementType,
            UomCode = movement.UomCode,
            Quantity = movement.Quantity,
            OnHandQuantityDelta = movement.OnHandQuantityDelta,
            ReservedQuantityDelta = movement.ReservedQuantityDelta,
            ReferenceType = movement.ReferenceType,
            ReferenceId = movement.ReferenceId,
            IdempotencyKey = movement.IdempotencyKey,
            CorrelationId = movement.CorrelationId,
            OperationId = movement.OperationId,
            OccurredAtUtc = timeProvider.GetUtcNow(),
        });
        if (movement.ReferenceType == "MANUAL")
        {
            AddReleaseUsageOutbox(item.ProductId, movement.OperationId);
        }
    }

    private static StockMovementType ToMovementType(IStockMovementEvent movementEvent)
    {
        return movementEvent switch
        {
            StockReceived => StockMovementType.Receipt,
            StockAdjusted => StockMovementType.Adjustment,
            StockDeducted => StockMovementType.Deduction,
            StockReserved => StockMovementType.Reserved,
            StockCommitted => StockMovementType.Committed,
            StockReleased => StockMovementType.Released,
            _ => throw new InvalidOperationException($"Unsupported movement event '{movementEvent.GetType().Name}'."),
        };
    }

    private static StockItemView ToView(StockItemReadEntity entity)
    {
        return new StockItemView(
            entity.Id,
            entity.ProductId,
            entity.BaseUomCode,
            entity.OnHandQuantity,
            entity.ReservedQuantity,
            entity.OnHandQuantity - entity.ReservedQuantity);
    }

    private static StockMovementView ToView(StockMovementReadEntity entity)
    {
        return new StockMovementView(
            entity.Id,
            entity.StockItemId,
            entity.Type,
            entity.UomCode,
            entity.Quantity,
            entity.OnHandQuantityDelta,
            entity.ReservedQuantityDelta,
            entity.ReferenceType,
            entity.ReferenceId,
            entity.IdempotencyKey,
            entity.CorrelationId,
            entity.OccurredAtUtc);
    }

    private void AddReleaseUsageOutbox(Guid productId, Guid operationId)
    {
        if (dbContext.OutboxMessages.Local.Any(item => item.ProductId == productId && item.OperationId == operationId))
        {
            return;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        dbContext.OutboxMessages.Add(new InventoryOutboxMessageEntity
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            OperationId = operationId,
            OccurredAtUtc = now,
            NextAttemptAtUtc = now,
        });
    }
}
