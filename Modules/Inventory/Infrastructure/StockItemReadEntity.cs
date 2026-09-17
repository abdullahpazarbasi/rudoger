namespace Rudoger.Modules.Inventory.Infrastructure;

public sealed class StockItemReadEntity
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string BaseUomCode { get; set; } = string.Empty;

    public decimal OpeningQuantity { get; set; }

    public string OpeningUomCode { get; set; } = string.Empty;

    public decimal RequestedOpeningQuantity { get; set; }

    public string CreationIdempotencyKey { get; set; } = string.Empty;

    public decimal OnHandQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }
}
