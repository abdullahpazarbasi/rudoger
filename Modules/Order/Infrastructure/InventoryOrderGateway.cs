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
        Guid operationId,
        CancellationToken cancellationToken)
    {
        return TranslateAsync(() => callLogger.ExecuteAsync(
            "Inventory.Reserve",
            token => inventoryApi.ReserveAsync(productId, quantity, orderId, operationId, token),
            cancellationToken));
    }

    public Task CommitAsync(Guid productId, Guid orderId, Guid operationId, CancellationToken cancellationToken)
    {
        return TranslateAsync(() => callLogger.ExecuteAsync(
            "Inventory.Commit",
            token => inventoryApi.CommitAsync(productId, orderId, operationId, token),
            cancellationToken));
    }

    public Task ReleaseAsync(Guid productId, Guid orderId, Guid operationId, CancellationToken cancellationToken)
    {
        return TranslateAsync(() => callLogger.ExecuteAsync(
            "Inventory.Release",
            token => inventoryApi.ReleaseAsync(productId, orderId, operationId, token),
            cancellationToken));
    }

    public Task CompensateReservationAsync(
        Guid productId,
        Guid orderId,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        return TranslateAsync(() => callLogger.ExecuteAsync(
            "Inventory.CompensateReservation",
            token => inventoryApi.CompensateReservationAsync(productId, orderId, operationId, token),
            cancellationToken));
    }

    private static async Task TranslateAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (Exception exception) when (OrderGatewayFailure.IsPublishedFailure(exception))
        {
            throw OrderGatewayFailure.FromInventory(exception);
        }
    }
}
