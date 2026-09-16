namespace Rudoger.Modules.Inventory.Domain;

public sealed record StockDeducted(StockMovementData Movement) : IStockMovementEvent;
