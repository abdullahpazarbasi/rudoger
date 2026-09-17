using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Inventory.Application;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.Modules.Inventory.Infrastructure;

public sealed class ProductInventoryGateway(IProductInternalApi productApi, IInternalCallLogger callLogger) : IProductInventoryGateway
{
    public async Task<InventoryProductOffer> ClaimOfferAsync(
        Guid productId,
        Guid operationId,
        string uomCode,
        CancellationToken cancellationToken)
    {
        string normalizedUomCode = uomCode.Trim().ToUpperInvariant();
        ProductOfferContract offer = await TranslateAsync(() => callLogger.ExecuteAsync(
            "Product.ClaimInventoryUsage",
            token => productApi.ClaimOfferAsync(
                productId,
                operationId,
                ProductUsageType.Inventory,
                [normalizedUomCode],
                token),
            cancellationToken));
        return new InventoryProductOffer(
            offer.BaseUomCode,
            normalizedUomCode,
            offer.ConversionFactors[normalizedUomCode]);
    }

    public Task ReleaseUsageAsync(Guid productId, Guid operationId, CancellationToken cancellationToken)
    {
        return TranslateAsync(() => callLogger.ExecuteAsync(
            "Product.ReleaseInventoryUsage",
            token => productApi.ReleaseUsageAsync(productId, operationId, token),
            cancellationToken));
    }

    private static async Task<T> TranslateAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (Exception exception) when (InventoryGatewayFailure.IsPublishedFailure(exception))
        {
            throw InventoryGatewayFailure.FromProduct(exception);
        }
    }

    private static async Task TranslateAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (Exception exception) when (InventoryGatewayFailure.IsPublishedFailure(exception))
        {
            throw InventoryGatewayFailure.FromProduct(exception);
        }
    }
}
