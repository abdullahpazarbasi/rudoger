using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Presentation;

/// <summary>
/// The failure codes the inventory internal API publishes to its callers. A caller translates these
/// into its own vocabulary instead of forwarding them, so the stock model stays behind the gateway.
/// </summary>
public static class InventoryApiFailureCode
{
    public const string StockItemNotFound = InventoryFailureCode.StockItemNotFound;

    public const string StockItemMissingForProduct = InventoryFailureCode.StockItemMissingForProduct;

    public const string InsufficientStock = InventoryFailureCode.InsufficientStock;

    public const string ReservationNotFound = InventoryFailureCode.ReservationNotFound;
}
