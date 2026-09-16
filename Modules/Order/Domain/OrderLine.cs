namespace Rudoger.Modules.Order.Domain;

public sealed class OrderLine
{
    public required Guid Id { get; init; }

    public required int Num { get; init; }

    public required Guid ProductId { get; init; }

    public required string UomCode { get; init; }

    public required decimal Quantity { get; init; }

    public required decimal UnitPriceAmount { get; init; }

    public required string UnitPriceCurrencyCode { get; init; }

    public required decimal BaseQuantity { get; init; }
}
