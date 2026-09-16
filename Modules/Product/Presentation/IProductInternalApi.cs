namespace Rudoger.Modules.Product.Presentation;

public interface IProductInternalApi
{
    Task<ProductOfferContract> ClaimOfferAsync(
        Guid productId,
        Guid operationId,
        string usageType,
        IReadOnlyCollection<string> uomCodes,
        CancellationToken cancellationToken);

    Task ReleaseUsageAsync(Guid productId, Guid operationId, CancellationToken cancellationToken);
}
