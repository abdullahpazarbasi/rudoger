using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Order.Application;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.Modules.Order.Infrastructure;

public sealed class ProductOrderGateway(IProductInternalApi productApi, IInternalCallLogger callLogger) : IProductOrderGateway
{
    public async Task<OrderProductOffer> ClaimOfferAsync(
        Guid productId,
        Guid operationId,
        IReadOnlyCollection<string> uomCodes,
        CancellationToken cancellationToken)
    {
        ProductOfferContract offer = await TranslateAsync(() => callLogger.ExecuteAsync(
            "Product.ClaimOrderUsage",
            token => productApi.ClaimOfferAsync(
                productId,
                operationId,
                ProductUsageType.OrderPlacement,
                uomCodes,
                token),
            cancellationToken));
        return new OrderProductOffer(
            offer.ProductId,
            offer.BasePriceAmount,
            offer.CurrencyCode,
            offer.ConversionFactors);
    }

    public Task ReleaseUsageAsync(Guid productId, Guid operationId, CancellationToken cancellationToken)
    {
        return TranslateAsync(() => callLogger.ExecuteAsync(
            "Product.ReleaseOrderUsage",
            token => productApi.ReleaseUsageAsync(productId, operationId, token),
            cancellationToken));
    }

    private static async Task<T> TranslateAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (Exception exception) when (OrderGatewayFailure.IsPublishedFailure(exception))
        {
            throw OrderGatewayFailure.FromProduct(exception);
        }
    }

    private static async Task TranslateAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (Exception exception) when (OrderGatewayFailure.IsPublishedFailure(exception))
        {
            throw OrderGatewayFailure.FromProduct(exception);
        }
    }
}
