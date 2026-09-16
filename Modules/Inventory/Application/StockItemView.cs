namespace Rudoger.Modules.Inventory.Application;

public sealed record StockItemView(
    Guid Id,
    Guid ProductId,
    string BaseUomCode,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity);
