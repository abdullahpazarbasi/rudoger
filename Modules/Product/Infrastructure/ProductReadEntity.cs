namespace Rudoger.Modules.Product.Infrastructure;

public sealed class ProductReadEntity
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string BaseUomCode { get; set; } = string.Empty;

    public decimal BasePriceAmount { get; set; }

    public string BasePriceCurrencyCode { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public List<ProductPackagingReadEntity> Packagings { get; set; } = [];
}
