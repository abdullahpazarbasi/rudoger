namespace Rudoger.Modules.Order.Application;

public sealed record OrderLineView(
    Guid Id,
    int Num,
    Guid ProductId,
    string UomCode,
    decimal Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrencyCode);
