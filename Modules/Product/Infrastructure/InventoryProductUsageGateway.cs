using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Inventory.Presentation;
using Rudoger.Modules.Product.Application;

namespace Rudoger.Modules.Product.Infrastructure;

public sealed class InventoryProductUsageGateway(IInventoryInternalApi inventoryApi, IInternalCallLogger callLogger)
    : IInventoryProductUsageGateway
{
    public Task<bool> HasAnyStockAsync(Guid productId, CancellationToken cancellationToken)
    {
        return callLogger.ExecuteAsync(
            "Inventory.HasAnyStock",
            token => inventoryApi.HasAnyStockAsync(productId, token),
            cancellationToken);
    }
}
