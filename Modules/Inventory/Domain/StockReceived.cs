namespace Rudoger.Modules.Inventory.Domain;

public sealed record StockReceived(StockMovementData Movement) : IStockMovementEvent;
