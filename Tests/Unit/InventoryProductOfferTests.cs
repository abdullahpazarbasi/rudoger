using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Inventory.Application;

namespace Rudoger.UnitTests;

public sealed class InventoryProductOfferTests
{
    [Fact]
    public void ConvertsRequestedUomQuantityToBaseQuantity()
    {
        var offer = new InventoryProductOffer("EA", "CASE", 10);

        Assert.Equal(25, offer.ToBaseQuantity(2.5m));
    }

    [Fact]
    public void RejectsQuantityOutsideDecimalRange()
    {
        var offer = new InventoryProductOffer("EA", "CASE", 2);

        DomainException exception = Assert.Throws<DomainException>(() => offer.ToBaseQuantity(decimal.MaxValue));
        Assert.Equal("stock-quantity-out-of-range", exception.Code);
    }
}
