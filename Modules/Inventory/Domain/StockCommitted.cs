namespace Rudoger.Modules.Inventory.Domain;

public sealed record StockCommitted(StockMovementData Movement) : IStockMovementEvent;
