using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Presentation;

public sealed record CreateStockMovementRequest(StockMovementTypeContract Type, string UomCode, decimal Quantity)
{
    public StockMovementType ToMovementType()
    {
        return Type switch
        {
            StockMovementTypeContract.Receipt => StockMovementType.Receipt,
            StockMovementTypeContract.Adjustment => StockMovementType.Adjustment,
            StockMovementTypeContract.Deduction => StockMovementType.Deduction,
            StockMovementTypeContract.Reserved => StockMovementType.Reserved,
            StockMovementTypeContract.Committed => StockMovementType.Committed,
            StockMovementTypeContract.Released => StockMovementType.Released,
            _ => throw new ValidationException(
                "stock-movement-type-invalid",
                $"Movement type '{Type}' is not supported."),
        };
    }
}
