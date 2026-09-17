namespace Rudoger.Modules.Order.Domain;

public static class OrderFailureCode
{
    public const string ProductUnavailable = "order-product-unavailable";

    public const string ProductUomUnavailable = "order-product-uom-unavailable";

    public const string ProductRejected = "order-product-rejected";

    public const string StockUnavailable = "order-stock-unavailable";

    public const string StockInsufficient = "order-stock-insufficient";

    public const string StockReservationMissing = "order-stock-reservation-missing";

    public const string StockRejected = "order-stock-rejected";
}
