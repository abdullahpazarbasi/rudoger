using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Inventory.Domain;
using Rudoger.Modules.Inventory.Infrastructure;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.UnitTests;

public sealed class InventoryGatewayFailureTests
{
    [Theory]
    [InlineData(ProductApiFailureCode.ProductNotFound, InventoryFailureCode.ProductUnavailable)]
    [InlineData(ProductApiFailureCode.ProductDeleted, InventoryFailureCode.ProductUnavailable)]
    [InlineData(ProductApiFailureCode.PackagingNotFound, InventoryFailureCode.ProductUomUnavailable)]
    [InlineData("usage-claim-conflict", InventoryFailureCode.ProductRejected)]
    public void ProductFailuresBecomeInventoryFailures(string productCode, string expectedCode)
    {
        var source = new DomainException(productCode, "Product context wording.");

        Exception translated = InventoryGatewayFailure.FromProduct(source);

        string translatedCode = translated switch
        {
            DomainException domain => domain.Code,
            ConflictException conflict => conflict.Code,
            _ => throw new InvalidOperationException("Unexpected translated exception type."),
        };

        Assert.Equal(expectedCode, translatedCode);
        Assert.StartsWith("stock-item-", translatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain(productCode, translated.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConcurrencyFailuresPassThrough()
    {
        Assert.False(InventoryGatewayFailure.IsPublishedFailure(new ConcurrencyException(Guid.CreateVersion7())));
    }
}
