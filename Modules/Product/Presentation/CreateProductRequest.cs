namespace Rudoger.Modules.Product.Presentation;

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string BaseUomCode,
    decimal BasePriceAmount,
    string BasePriceCurrencyCode,
    IReadOnlyList<ProductPackagingRequest> Packagings);
