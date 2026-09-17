using Rudoger.Modules.Inventory.Application;

namespace Rudoger.Modules.Inventory.Presentation;

public sealed record StockItemResponse(
    Guid Id,
    Guid ProductId,
    string BaseUomCode,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity)
{
    public static StockItemResponse From(StockItemView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new StockItemResponse(
            view.Id,
            view.ProductId,
            view.BaseUomCode,
            view.OnHandQuantity,
            view.ReservedQuantity,
            view.AvailableQuantity);
    }
}
