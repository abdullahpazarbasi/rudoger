namespace Rudoger.Modules.Inventory.Domain;

public sealed record StockAdjusted(StockMovementData Movement) : IStockMovementEvent;
