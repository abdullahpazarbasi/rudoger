namespace Rudoger.Modules.Inventory.Application;

public sealed record CreateStockItemCommand(Guid ProductId, decimal OpeningQuantity, string IdempotencyKey);
