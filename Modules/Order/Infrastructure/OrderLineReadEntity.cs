namespace Rudoger.Modules.Order.Infrastructure;

public sealed class OrderLineReadEntity
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public int Num { get; set; }

    public Guid ProductId { get; set; }

    public string UomCode { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPriceAmount { get; set; }

    public string UnitPriceCurrencyCode { get; set; } = string.Empty;

    public decimal BaseQuantity { get; set; }

    public OrderReadEntity Order { get; set; } = null!;
}
