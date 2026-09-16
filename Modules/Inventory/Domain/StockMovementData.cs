namespace Rudoger.Modules.Inventory.Domain;

public sealed record StockMovementData(
    Guid MovementId,
    decimal OnHandQuantityDelta,
    decimal ReservedQuantityDelta,
    string ReferenceType,
    Guid? ReferenceId,
    string IdempotencyKey,
    string CorrelationId,
    Guid SourceEventId);
