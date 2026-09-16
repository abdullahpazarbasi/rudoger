namespace Rudoger.Modules.Order.Domain;

public sealed record OrderLineDefinition(
    Guid Id,
    int Num,
    Guid ProductId,
    string UomCode,
    decimal Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrencyCode,
    decimal BaseQuantity);
