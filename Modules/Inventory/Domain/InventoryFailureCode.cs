namespace Rudoger.Modules.Inventory.Domain;

public static class InventoryFailureCode
{
    public const string StockItemNotFound = "stock-item-not-found";

    public const string StockItemMissingForProduct = "stock-item-missing-for-product";

    public const string InsufficientStock = "insufficient-stock";

    public const string ReservationNotFound = "stock-reservation-not-found";

    public const string ReservationConflict = "stock-reservation-conflict";

    public const string ReservationAlreadyCompensated = "stock-reservation-already-compensated";

    public const string ProductUnavailable = "stock-item-product-unavailable";

    public const string ProductUomUnavailable = "stock-item-uom-unavailable";

    public const string ProductRejected = "stock-item-product-rejected";
}
