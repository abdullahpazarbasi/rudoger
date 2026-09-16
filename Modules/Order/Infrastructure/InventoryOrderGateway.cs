using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Inventory.Presentation;
using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Infrastructure;

public sealed class InventoryOrderGateway(IInventoryInternalApi inventoryApi, IInternalCallLogger callLogger)
    : IInventoryOrderGateway
{
    public Task ReserveAsync(
        Guid productId,
        decimal quantity,
        Guid orderId,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        return callLogger.ExecuteAsync(
            "Inventory.Reserve",
            token => inventoryApi.ReserveAsync(productId, quantity, orderId, sourceEventId, token),
            cancellationToken);
    }

    public Task CommitAsync(Guid productId, Guid orderId, Guid sourceEventId, CancellationToken cancellationToken)
    {
        return callLogger.ExecuteAsync(
            "Inventory.Commit",
            token => inventoryApi.CommitAsync(productId, orderId, sourceEventId, token),
            cancellationToken);
    }

    public Task ReleaseAsync(Guid productId, Guid orderId, Guid sourceEventId, CancellationToken cancellationToken)
    {
        return callLogger.ExecuteAsync(
            "Inventory.Release",
            token => inventoryApi.ReleaseAsync(productId, orderId, sourceEventId, token),
            cancellationToken);
    }

    public Task CompensateReservationAsync(
        Guid productId,
        Guid orderId,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        return callLogger.ExecuteAsync(
            "Inventory.CompensateReservation",
            token => inventoryApi.CompensateReservationAsync(productId, orderId, sourceEventId, token),
            cancellationToken);
    }
}
