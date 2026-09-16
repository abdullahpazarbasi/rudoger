namespace Rudoger.Modules.Inventory.Domain;

public sealed record StockReleased(StockMovementData Movement) : IStockMovementEvent;
