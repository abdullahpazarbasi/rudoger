using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Infrastructure;

public sealed class StockMovementReadEntity
{
    public Guid Id { get; set; }

    public Guid StockItemId { get; set; }

    public StockMovementType Type { get; set; }

    public string UomCode { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal OnHandQuantityDelta { get; set; }

    public decimal ReservedQuantityDelta { get; set; }

    public string ReferenceType { get; set; } = string.Empty;

    public Guid? ReferenceId { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public Guid OperationId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
