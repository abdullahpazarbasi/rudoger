using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Application;

public sealed class InventoryInternalService(
    IInventoryRepository repository,
    ICorrelationContextAccessor correlationContextAccessor)
{
    public async Task ReserveAsync(
        Guid productId,
        decimal quantity,
        Guid orderId,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        StockItemAggregate aggregate = await LoadByProductRequiredAsync(productId, cancellationToken);
        aggregate.Reserve(quantity, orderId, correlationContextAccessor.Current.CorrelationId, sourceEventId);
        await repository.SaveAsync(aggregate, cancellationToken);
    }

    public async Task CommitAsync(
        Guid productId,
        Guid orderId,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        StockItemAggregate aggregate = await LoadByProductRequiredAsync(productId, cancellationToken);
        aggregate.Commit(orderId, correlationContextAccessor.Current.CorrelationId, sourceEventId);
        await repository.SaveAsync(aggregate, cancellationToken);
    }

    public async Task ReleaseAsync(
        Guid productId,
        Guid orderId,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        StockItemAggregate aggregate = await LoadByProductRequiredAsync(productId, cancellationToken);
        aggregate.Release(orderId, correlationContextAccessor.Current.CorrelationId, sourceEventId);
        await repository.SaveAsync(aggregate, cancellationToken);
    }

    public async Task CompensateReservationAsync(
        Guid productId,
        Guid orderId,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        StockItemAggregate? aggregate = await repository.LoadByProductAsync(productId, cancellationToken);
        if (aggregate is null)
        {
            return;
        }

        aggregate.ReleaseIfPresent(orderId, correlationContextAccessor.Current.CorrelationId, sourceEventId);
        await repository.SaveAsync(aggregate, cancellationToken);
    }

    public async Task<bool> HasAnyStockAsync(Guid productId, CancellationToken cancellationToken)
    {
        StockItemView? item = await repository.GetByProductAsync(productId, cancellationToken);
        return item is not null && (item.OnHandQuantity != 0 || item.ReservedQuantity != 0);
    }

    private async Task<StockItemAggregate> LoadByProductRequiredAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await repository.LoadByProductAsync(productId, cancellationToken)
            ?? throw new ConflictException("stock-item-not-found", $"No stock item exists for product '{productId}'.");
    }
}
