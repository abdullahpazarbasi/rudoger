using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Application;

public sealed record CreateStockMovementCommand(
    StockMovementType Type,
    string UomCode,
    decimal Quantity,
    string IdempotencyKey);
