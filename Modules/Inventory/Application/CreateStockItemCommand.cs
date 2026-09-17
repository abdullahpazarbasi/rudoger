namespace Rudoger.Modules.Inventory.Application;

public sealed record CreateStockItemCommand(
    Guid ProductId,
    string UomCode,
    decimal OpeningQuantity,
    string IdempotencyKey);
