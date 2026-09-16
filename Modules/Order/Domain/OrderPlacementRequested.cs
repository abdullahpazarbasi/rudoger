using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Order.Domain;

public sealed record OrderPlacementRequested(
    Guid PlacementId,
    Guid OrderId,
    Guid UserId,
    string IdempotencyKey,
    IReadOnlyList<OrderPlacementLine> Lines) : IDomainEvent;
