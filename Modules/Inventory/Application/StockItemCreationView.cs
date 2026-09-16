namespace Rudoger.Modules.Inventory.Application;

public sealed record StockItemCreationView(
    StockItemView StockItem,
    decimal OpeningQuantity,
    string IdempotencyKey);
