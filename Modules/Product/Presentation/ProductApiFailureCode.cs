using Rudoger.Modules.Product.Domain;

namespace Rudoger.Modules.Product.Presentation;

/// <summary>
/// The failure codes the product internal API publishes to its callers. A caller translates these
/// into its own vocabulary instead of forwarding them, so the product model stays behind the gateway.
/// </summary>
public static class ProductApiFailureCode
{
    public const string ProductNotFound = ProductFailureCode.ProductNotFound;

    public const string ProductDeleted = ProductFailureCode.ProductDeleted;

    public const string PackagingNotFound = ProductFailureCode.PackagingNotFound;
}
