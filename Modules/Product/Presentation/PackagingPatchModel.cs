namespace Rudoger.Modules.Product.Presentation;

public sealed class PackagingPatchModel
{
    public int Level { get; set; }

    public string UomCode { get; set; } = string.Empty;

    public decimal ConversionFactor { get; set; }

    public string? Barcode { get; set; }

    public decimal? WeightInKg { get; set; }

    public decimal? LengthInMm { get; set; }

    public decimal? WidthInMm { get; set; }

    public decimal? HeightInMm { get; set; }
}
