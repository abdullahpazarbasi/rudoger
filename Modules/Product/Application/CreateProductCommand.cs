namespace Rudoger.Modules.Product.Application;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string BaseUomCode,
    decimal BasePriceAmount,
    string BasePriceCurrencyCode,
    IReadOnlyList<PackagingInput> Packagings);
