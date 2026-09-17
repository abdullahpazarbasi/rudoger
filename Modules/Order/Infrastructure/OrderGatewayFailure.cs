using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Inventory.Presentation;
using Rudoger.Modules.Order.Domain;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.Modules.Order.Infrastructure;

/// <summary>
/// Translates the failures published by the product and inventory internal APIs into the order
/// vocabulary. The order context never forwards another context's failure code or message, so
/// nothing the order API exposes reveals how the product or stock model is organised.
/// </summary>
public static class OrderGatewayFailure
{
    public static bool IsPublishedFailure(Exception exception)
    {
        return exception is DomainException or ConflictException or NotFoundException;
    }

    public static ConflictException FromProduct(Exception exception)
    {
        return CodeOf(exception) switch
        {
            ProductApiFailureCode.ProductNotFound or ProductApiFailureCode.ProductDeleted =>
                new ConflictException(
                    OrderFailureCode.ProductUnavailable,
                    "An ordered product is not available."),
            ProductApiFailureCode.PackagingNotFound => new ConflictException(
                OrderFailureCode.ProductUomUnavailable,
                "An order line requests a unit of measure the ordered product does not offer."),
            _ => new ConflictException(
                OrderFailureCode.ProductRejected,
                "An ordered product rejected this order."),
        };
    }

    public static ConflictException FromInventory(Exception exception)
    {
        return CodeOf(exception) switch
        {
            InventoryApiFailureCode.StockItemNotFound
                or InventoryApiFailureCode.StockItemMissingForProduct => new ConflictException(
                    OrderFailureCode.StockUnavailable,
                    "An ordered product has no stock to draw from."),
            InventoryApiFailureCode.InsufficientStock => new ConflictException(
                OrderFailureCode.StockInsufficient,
                "One or more order lines exceed the quantity available for the ordered product."),
            InventoryApiFailureCode.ReservationNotFound => new ConflictException(
                OrderFailureCode.StockReservationMissing,
                "The quantity held for this order is no longer reserved."),
            _ => new ConflictException(
                OrderFailureCode.StockRejected,
                "The stock operation this order requires was rejected."),
        };
    }

    private static string? CodeOf(Exception exception)
    {
        return exception switch
        {
            DomainException domain => domain.Code,
            ConflictException conflict => conflict.Code,
            NotFoundException notFound => notFound.Code,
            _ => null,
        };
    }
}
