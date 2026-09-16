using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Application;

public sealed record CreateStockMovementCommand(StockMovementType Type, decimal Quantity, string IdempotencyKey);
