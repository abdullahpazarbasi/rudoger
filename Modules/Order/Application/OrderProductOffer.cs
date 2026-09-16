namespace Rudoger.Modules.Order.Application;

public sealed record OrderProductOffer(
    Guid ProductId,
    decimal BasePriceAmount,
    string CurrencyCode,
    IReadOnlyDictionary<string, decimal> ConversionFactors);
