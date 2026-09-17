using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Presentation;

public sealed record CreateOrderTransitionRequest(OrderTransitionTargetContract Target)
{
    public OrderTransitionTarget ToTarget()
    {
        return Target switch
        {
            OrderTransitionTargetContract.Shipped => OrderTransitionTarget.Shipped,
            OrderTransitionTargetContract.Cancelled => OrderTransitionTarget.Cancelled,
            _ => throw new ValidationException(
                "order-transition-target-invalid",
                $"Transition target '{Target}' is not supported."),
        };
    }
}
