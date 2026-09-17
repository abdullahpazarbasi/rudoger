using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Presentation;

public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    OrderStatusContract Status,
    Guid UserId,
    IReadOnlyList<OrderLineResponse> Lines,
    Guid? PendingTransitionId,
    OrderTransitionTargetContract? PendingTransitionTarget)
{
    public static OrderResponse From(OrderView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new OrderResponse(
            view.Id,
            view.OrderNumber,
            OrderContract.ToStatus(view.Status),
            view.UserId,
            view.Lines.Select(OrderLineResponse.From).ToArray(),
            view.PendingTransitionId,
            view.PendingTransitionTarget is null
                ? null
                : OrderContract.ToTransitionTarget(view.PendingTransitionTarget.Value));
    }
}
