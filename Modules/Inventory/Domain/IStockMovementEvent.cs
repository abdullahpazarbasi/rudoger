using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Inventory.Domain;

public interface IStockMovementEvent : IDomainEvent
{
    StockMovementData Movement { get; }
}
