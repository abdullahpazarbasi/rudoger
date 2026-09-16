namespace Rudoger.Modules.Product.Application;

public sealed record ChangeProductCommand(
    string Sku,
    string Name,
    decimal BasePriceAmount,
    string BasePriceCurrencyCode);
