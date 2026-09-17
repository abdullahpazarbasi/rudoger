using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Presentation;

public sealed record OrderTransitionResponse(
    Guid Id,
    Guid OrderId,
    OrderTransitionTargetContract Target,
    OrderTransitionStatusContract Status)
{
    public static OrderTransitionResponse From(OrderTransitionView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new OrderTransitionResponse(
            view.Id,
            view.OrderId,
            OrderContract.ToTransitionTarget(view.Target),
            OrderContract.ToTransitionStatus(view.Status));
    }
}
