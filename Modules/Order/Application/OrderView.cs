using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Application;

public sealed record OrderView(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    Guid UserId,
    IReadOnlyList<OrderLineView> Lines,
    Guid? PendingTransitionId,
    OrderTransitionTarget? PendingTransitionTarget);
