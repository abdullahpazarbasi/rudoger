namespace Rudoger.Modules.Product.Presentation;

public sealed class ProductPatchModel
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal BasePriceAmount { get; set; }

    public string BasePriceCurrencyCode { get; set; } = string.Empty;
}
