using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Presentation;

/// <summary>
/// Converts between the order model and the vocabulary the order API publishes, so the wire format
/// never binds a caller to the order context's own enumerations.
/// </summary>
public static class OrderContract
{
    public static OrderStatusContract ToStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Placed => OrderStatusContract.Placed,
            OrderStatus.Shipped => OrderStatusContract.Shipped,
            OrderStatus.Cancelled => OrderStatusContract.Cancelled,
            _ => throw new InvalidOperationException($"Unsupported order status '{status}'."),
        };
    }

    public static OrderTransitionTargetContract ToTransitionTarget(OrderTransitionTarget target)
    {
        return target switch
        {
            OrderTransitionTarget.Shipped => OrderTransitionTargetContract.Shipped,
            OrderTransitionTarget.Cancelled => OrderTransitionTargetContract.Cancelled,
            _ => throw new InvalidOperationException($"Unsupported order transition target '{target}'."),
        };
    }

    public static OrderPlacementStatusContract ToPlacementStatus(OrderPlacementStatus status)
    {
        return status switch
        {
            OrderPlacementStatus.Pending => OrderPlacementStatusContract.Pending,
            OrderPlacementStatus.Succeeded => OrderPlacementStatusContract.Succeeded,
            OrderPlacementStatus.Failed => OrderPlacementStatusContract.Failed,
            _ => throw new InvalidOperationException($"Unsupported order placement status '{status}'."),
        };
    }

    public static OrderTransitionStatusContract ToTransitionStatus(OrderTransitionStatus status)
    {
        return status switch
        {
            OrderTransitionStatus.Pending => OrderTransitionStatusContract.Pending,
            OrderTransitionStatus.Succeeded => OrderTransitionStatusContract.Succeeded,
            _ => throw new InvalidOperationException($"Unsupported order transition status '{status}'."),
        };
    }
}
