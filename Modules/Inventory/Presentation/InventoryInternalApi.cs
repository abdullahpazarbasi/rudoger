using Rudoger.Modules.Inventory.Application;

namespace Rudoger.Modules.Inventory.Presentation;

public sealed class InventoryInternalApi(InventoryInternalService service) : IInventoryInternalApi
{
    public Task ReserveAsync(
        Guid productId,
        decimal quantity,
        Guid orderId,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        return service.ReserveAsync(productId, quantity, orderId, operationId, cancellationToken);
    }

    public Task CommitAsync(Guid productId, Guid orderId, Guid operationId, CancellationToken cancellationToken)
    {
        return service.CommitAsync(productId, orderId, operationId, cancellationToken);
    }

    public Task ReleaseAsync(Guid productId, Guid orderId, Guid operationId, CancellationToken cancellationToken)
    {
        return service.ReleaseAsync(productId, orderId, operationId, cancellationToken);
    }

    public Task CompensateReservationAsync(
        Guid productId,
        Guid orderId,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        return service.CompensateReservationAsync(productId, orderId, operationId, cancellationToken);
    }

    public Task<bool> HasAnyStockAsync(Guid productId, CancellationToken cancellationToken)
    {
        return service.HasAnyStockAsync(productId, cancellationToken);
    }
}
