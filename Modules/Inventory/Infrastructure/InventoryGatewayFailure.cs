using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Inventory.Domain;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.Modules.Inventory.Infrastructure;

/// <summary>
/// Translates the failures published by the product internal API into the inventory vocabulary, so
/// the inventory API never exposes how the product context names or describes its own rules.
/// </summary>
public static class InventoryGatewayFailure
{
    public static bool IsPublishedFailure(Exception exception)
    {
        return exception is DomainException or ConflictException or NotFoundException;
    }

    public static Exception FromProduct(Exception exception)
    {
        return CodeOf(exception) switch
        {
            ProductApiFailureCode.ProductNotFound or ProductApiFailureCode.ProductDeleted =>
                new ConflictException(
                    InventoryFailureCode.ProductUnavailable,
                    "The product this stock item refers to is not available."),
            ProductApiFailureCode.PackagingNotFound => new DomainException(
                InventoryFailureCode.ProductUomUnavailable,
                "The requested UoM code is not offered for this product."),
            _ => new ConflictException(
                InventoryFailureCode.ProductRejected,
                "The product rejected this stock operation."),
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
