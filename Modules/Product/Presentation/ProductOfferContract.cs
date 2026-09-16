namespace Rudoger.Modules.Product.Presentation;

public sealed record ProductOfferContract(
    Guid ProductId,
    string BaseUomCode,
    decimal BasePriceAmount,
    string CurrencyCode,
    IReadOnlyDictionary<string, decimal> ConversionFactors);
