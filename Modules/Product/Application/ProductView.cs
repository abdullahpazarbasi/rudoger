namespace Rudoger.Modules.Product.Application;

public sealed record ProductView(
    Guid Id,
    string Sku,
    string Name,
    string BaseUomCode,
    decimal BasePriceAmount,
    string BasePriceCurrencyCode,
    IReadOnlyList<ProductPackagingView> Packagings);
