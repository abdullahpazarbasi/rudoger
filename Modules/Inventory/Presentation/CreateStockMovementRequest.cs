using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Presentation;

public sealed record CreateStockMovementRequest(StockMovementType Type, decimal Quantity);
