namespace Rudoger.Modules.Inventory.Application;

public sealed record StockItemCreationView(
    StockItemView StockItem,
    string UomCode,
    decimal OpeningQuantity,
    string IdempotencyKey);
