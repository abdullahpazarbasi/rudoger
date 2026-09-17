using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Inventory.Presentation;
using Rudoger.Modules.Order.Domain;
using Rudoger.Modules.Order.Infrastructure;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.UnitTests;

public sealed class OrderGatewayFailureTests
{
    [Theory]
    [InlineData(ProductApiFailureCode.ProductNotFound, OrderFailureCode.ProductUnavailable)]
    [InlineData(ProductApiFailureCode.ProductDeleted, OrderFailureCode.ProductUnavailable)]
    [InlineData(ProductApiFailureCode.PackagingNotFound, OrderFailureCode.ProductUomUnavailable)]
    [InlineData("usage-claim-conflict", OrderFailureCode.ProductRejected)]
    [InlineData("some-unmapped-product-code", OrderFailureCode.ProductRejected)]
    public void ProductFailuresBecomeOrderFailures(string productCode, string expectedCode)
    {
        var source = new ConflictException(productCode, "Product context wording.");

        ConflictException translated = OrderGatewayFailure.FromProduct(source);

        Assert.Equal(expectedCode, translated.Code);
        Assert.NotEqual(source.Message, translated.Message);
    }

    [Theory]
    [InlineData(InventoryApiFailureCode.StockItemNotFound, OrderFailureCode.StockUnavailable)]
    [InlineData(InventoryApiFailureCode.StockItemMissingForProduct, OrderFailureCode.StockUnavailable)]
    [InlineData(InventoryApiFailureCode.InsufficientStock, OrderFailureCode.StockInsufficient)]
    [InlineData(InventoryApiFailureCode.ReservationNotFound, OrderFailureCode.StockReservationMissing)]
    [InlineData("stock-reservation-conflict", OrderFailureCode.StockRejected)]
    [InlineData("some-unmapped-stock-code", OrderFailureCode.StockRejected)]
    public void InventoryFailuresBecomeOrderFailures(string inventoryCode, string expectedCode)
    {
        var source = new ConflictException(inventoryCode, "Inventory context wording.");

        ConflictException translated = OrderGatewayFailure.FromInventory(source);

        Assert.Equal(expectedCode, translated.Code);
        Assert.NotEqual(source.Message, translated.Message);
    }

    [Fact]
    public void EveryTranslatedFailureUsesTheOrderVocabulary()
    {
        string[] foreignCodes =
        [
            ProductApiFailureCode.ProductNotFound,
            ProductApiFailureCode.ProductDeleted,
            ProductApiFailureCode.PackagingNotFound,
            InventoryApiFailureCode.StockItemNotFound,
            InventoryApiFailureCode.StockItemMissingForProduct,
            InventoryApiFailureCode.InsufficientStock,
            InventoryApiFailureCode.ReservationNotFound,
            "anything-else",
        ];

        foreach (string code in foreignCodes)
        {
            var source = new DomainException(code, $"Wording owned by the {code} context.");

            foreach (ConflictException translated in new[]
            {
                OrderGatewayFailure.FromProduct(source),
                OrderGatewayFailure.FromInventory(source),
            })
            {
                Assert.StartsWith("order-", translated.Code, StringComparison.Ordinal);
                Assert.DoesNotContain(code, translated.Message, StringComparison.Ordinal);
            }
        }
    }

    [Theory]
    [InlineData(typeof(DomainException))]
    [InlineData(typeof(ConflictException))]
    [InlineData(typeof(NotFoundException))]
    public void PublishedFailuresAreTranslatable(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "code", "message")!;

        Assert.True(OrderGatewayFailure.IsPublishedFailure(exception));
    }

    [Fact]
    public void ConcurrencyAndUnexpectedFailuresPassThrough()
    {
        Assert.False(OrderGatewayFailure.IsPublishedFailure(new ConcurrencyException(Guid.CreateVersion7())));
        Assert.False(OrderGatewayFailure.IsPublishedFailure(new InvalidOperationException("boom")));
    }

    [Fact]
    public void NotFoundFailuresAreTranslatedByTheirCode()
    {
        ConflictException translated = OrderGatewayFailure.FromProduct(
            new NotFoundException(ProductApiFailureCode.ProductNotFound, "Product 'x' was not found."));

        Assert.Equal(OrderFailureCode.ProductUnavailable, translated.Code);
    }
}
