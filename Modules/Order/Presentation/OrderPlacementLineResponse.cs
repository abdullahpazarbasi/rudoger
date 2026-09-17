using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Presentation;

public sealed record OrderPlacementLineResponse(int Num, Guid ProductId, string UomCode, decimal Quantity)
{
    public static OrderPlacementLineResponse From(OrderPlacementLineView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new OrderPlacementLineResponse(view.Num, view.ProductId, view.UomCode, view.Quantity);
    }
}
