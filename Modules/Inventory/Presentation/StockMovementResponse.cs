using Rudoger.Modules.Inventory.Application;
using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Presentation;

public sealed record StockMovementResponse(
    Guid Id,
    Guid StockItemId,
    StockMovementTypeContract Type,
    string UomCode,
    decimal Quantity,
    decimal OnHandQuantityDelta,
    decimal ReservedQuantityDelta,
    string ReferenceType,
    Guid? ReferenceId,
    string IdempotencyKey,
    string CorrelationId,
    DateTimeOffset OccurredAtUtc)
{
    public static StockMovementResponse From(StockMovementView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new StockMovementResponse(
            view.Id,
            view.StockItemId,
            ToContract(view.Type),
            view.UomCode,
            view.Quantity,
            view.OnHandQuantityDelta,
            view.ReservedQuantityDelta,
            view.ReferenceType,
            view.ReferenceId,
            view.IdempotencyKey,
            view.CorrelationId,
            view.OccurredAtUtc);
    }

    private static StockMovementTypeContract ToContract(StockMovementType type)
    {
        return type switch
        {
            StockMovementType.Receipt => StockMovementTypeContract.Receipt,
            StockMovementType.Adjustment => StockMovementTypeContract.Adjustment,
            StockMovementType.Deduction => StockMovementTypeContract.Deduction,
            StockMovementType.Reserved => StockMovementTypeContract.Reserved,
            StockMovementType.Committed => StockMovementTypeContract.Committed,
            StockMovementType.Released => StockMovementTypeContract.Released,
            _ => throw new InvalidOperationException($"Unsupported stock movement type '{type}'."),
        };
    }
}
