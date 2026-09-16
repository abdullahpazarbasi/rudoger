namespace Rudoger.Modules.Inventory.Domain;

public sealed record StockReserved(StockMovementData Movement) : IStockMovementEvent;
