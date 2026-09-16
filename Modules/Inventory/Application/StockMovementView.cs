using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Application;

public sealed record StockMovementView(
    Guid Id,
    Guid StockItemId,
    StockMovementType Type,
    decimal OnHandQuantityDelta,
    decimal ReservedQuantityDelta,
    string ReferenceType,
    Guid? ReferenceId,
    string IdempotencyKey,
    string CorrelationId,
    Guid SourceEventId,
    DateTimeOffset OccurredAtUtc);
