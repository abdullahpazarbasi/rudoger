using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Presentation;

public sealed record OrderPlacementResponse(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    OrderPlacementStatusContract Status,
    IReadOnlyList<OrderPlacementLineResponse> Lines,
    string? FailureCode,
    string? FailureDetail)
{
    public static OrderPlacementResponse From(OrderPlacementView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new OrderPlacementResponse(
            view.Id,
            view.OrderId,
            view.UserId,
            OrderContract.ToPlacementStatus(view.Status),
            view.Lines.Select(OrderPlacementLineResponse.From).ToArray(),
            view.FailureCode,
            view.FailureDetail);
    }
}
