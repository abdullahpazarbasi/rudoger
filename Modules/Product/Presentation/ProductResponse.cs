using Rudoger.Modules.Product.Application;

namespace Rudoger.Modules.Product.Presentation;

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string BaseUomCode,
    decimal BasePriceAmount,
    string BasePriceCurrencyCode,
    IReadOnlyList<ProductPackagingResponse> Packagings)
{
    public static ProductResponse From(ProductView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new ProductResponse(
            view.Id,
            view.Sku,
            view.Name,
            view.BaseUomCode,
            view.BasePriceAmount,
            view.BasePriceCurrencyCode,
            view.Packagings.Select(ProductPackagingResponse.From).ToArray());
    }
}
