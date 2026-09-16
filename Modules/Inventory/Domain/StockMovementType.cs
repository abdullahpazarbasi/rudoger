namespace Rudoger.Modules.Inventory.Domain;

public enum StockMovementType
{
    Receipt,
    Adjustment,
    Deduction,
    Reserved,
    Committed,
    Released,
}
