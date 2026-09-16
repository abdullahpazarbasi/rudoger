namespace Rudoger.Modules.Product.Application;

public sealed record ProductOffer(
    Guid ProductId,
    string BaseUomCode,
    decimal BasePriceAmount,
    string CurrencyCode,
    IReadOnlyDictionary<string, decimal> ConversionFactors);
