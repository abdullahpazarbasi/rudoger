using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Presentation;

public sealed record OrderLineResponse(
    Guid Id,
    int Num,
    Guid ProductId,
    string UomCode,
    decimal Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrencyCode)
{
    public static OrderLineResponse From(OrderLineView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new OrderLineResponse(
            view.Id,
            view.Num,
            view.ProductId,
            view.UomCode,
            view.Quantity,
            view.UnitPriceAmount,
            view.UnitPriceCurrencyCode);
    }
}
