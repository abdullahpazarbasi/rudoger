using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Product.Domain;

namespace Rudoger.Modules.Product.Application;

public sealed class ProductUsageApplicationService(IProductRepository repository)
{
    public async Task<ProductOffer> ClaimOfferAsync(
        Guid productId,
        Guid operationId,
        string usageType,
        IReadOnlyCollection<string> uomCodes,
        CancellationToken cancellationToken)
    {
        ProductAggregate aggregate = await LoadRequiredAsync(productId, cancellationToken);
        string[] normalizedCodes = uomCodes.Select(item => item.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        var factors = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (string code in normalizedCodes)
        {
            ProductPackaging packaging = aggregate.Packagings.SingleOrDefault(item => item.UomCode == code)
                ?? throw new DomainException(
                    ProductFailureCode.PackagingNotFound,
                    $"UoM code '{code}' is not available for product '{productId}'.");
            factors.Add(code, packaging.ConversionFactor);
        }

        aggregate.ClaimUsage(operationId, usageType);
        await repository.SaveAsync(aggregate, cancellationToken);
        return new ProductOffer(
            productId,
            aggregate.BaseUomCode,
            aggregate.BasePriceAmount,
            aggregate.BasePriceCurrencyCode,
            factors);
    }

    public async Task ReleaseUsageAsync(Guid productId, Guid operationId, CancellationToken cancellationToken)
    {
        ProductAggregate aggregate = await LoadRequiredAsync(productId, cancellationToken);
        aggregate.ReleaseUsage(operationId);
        await repository.SaveAsync(aggregate, cancellationToken);
    }

    private async Task<ProductAggregate> LoadRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        ProductAggregate? aggregate = await repository.LoadAsync(id, cancellationToken);
        if (aggregate is null || aggregate.IsDeleted)
        {
            throw new NotFoundException(ProductFailureCode.ProductNotFound, $"Product '{id}' was not found.");
        }

        return aggregate;
    }
}
