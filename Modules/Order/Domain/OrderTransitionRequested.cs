using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Order.Domain;

public sealed record OrderTransitionRequested(
    Guid TransitionId,
    OrderTransitionTarget Target,
    IReadOnlyList<TransitionStockOperation> StockOperations) : IDomainEvent;
