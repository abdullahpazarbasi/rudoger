using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Inventory.Application;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.Modules.Inventory.Infrastructure;

public sealed class ProductInventoryGateway(IProductInternalApi productApi, IInternalCallLogger callLogger) : IProductInventoryGateway
{
    public async Task<string> ClaimBaseUomAsync(Guid productId, Guid operationId, CancellationToken cancellationToken)
    {
        ProductOfferContract offer = await callLogger.ExecuteAsync(
            "Product.ClaimInventoryUsage",
            token => productApi.ClaimOfferAsync(productId, operationId, "INVENTORY", [], token),
            cancellationToken);
        return offer.BaseUomCode;
    }

    public Task ReleaseUsageAsync(Guid productId, Guid operationId, CancellationToken cancellationToken)
    {
        return callLogger.ExecuteAsync(
            "Product.ReleaseInventoryUsage",
            token => productApi.ReleaseUsageAsync(productId, operationId, token),
            cancellationToken);
    }
}
