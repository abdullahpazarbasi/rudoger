using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Inventory.Application;

public sealed record InventoryProductOffer(
    string BaseUomCode,
    string UomCode,
    decimal ConversionFactor)
{
    public decimal ToBaseQuantity(decimal quantity)
    {
        try
        {
            return checked(quantity * ConversionFactor);
        }
        catch (OverflowException)
        {
            throw new DomainException(
                "stock-quantity-out-of-range",
                $"The quantity in UoM '{UomCode}' exceeds the supported stock quantity range.");
        }
    }
}
