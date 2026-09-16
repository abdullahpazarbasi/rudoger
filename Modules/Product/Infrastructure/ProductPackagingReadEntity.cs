namespace Rudoger.Modules.Product.Infrastructure;

public sealed class ProductPackagingReadEntity
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int Level { get; set; }

    public string UomCode { get; set; } = string.Empty;

    public decimal ConversionFactor { get; set; }

    public string? Barcode { get; set; }

    public decimal? WeightInKg { get; set; }

    public decimal? LengthInMm { get; set; }

    public decimal? WidthInMm { get; set; }

    public decimal? HeightInMm { get; set; }

    public ProductReadEntity Product { get; set; } = null!;
}
