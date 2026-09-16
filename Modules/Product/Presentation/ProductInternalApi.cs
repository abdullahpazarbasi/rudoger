using Rudoger.Modules.Product.Application;

namespace Rudoger.Modules.Product.Presentation;

public sealed class ProductInternalApi(ProductUsageApplicationService service) : IProductInternalApi
{
    public async Task<ProductOfferContract> ClaimOfferAsync(
        Guid productId,
        Guid operationId,
        string usageType,
        IReadOnlyCollection<string> uomCodes,
        CancellationToken cancellationToken)
    {
        ProductOffer offer = await service.ClaimOfferAsync(
            productId,
            operationId,
            usageType,
            uomCodes,
            cancellationToken);
        return new ProductOfferContract(
            offer.ProductId,
            offer.BaseUomCode,
            offer.BasePriceAmount,
            offer.CurrencyCode,
            offer.ConversionFactors);
    }

    public Task ReleaseUsageAsync(Guid productId, Guid operationId, CancellationToken cancellationToken)
    {
        return service.ReleaseUsageAsync(productId, operationId, cancellationToken);
    }
}
