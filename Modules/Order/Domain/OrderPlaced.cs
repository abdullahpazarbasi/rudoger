using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Order.Domain;

public sealed record OrderPlaced(
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    IReadOnlyList<OrderLineDefinition> Lines) : IDomainEvent;
