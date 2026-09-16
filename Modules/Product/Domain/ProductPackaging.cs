namespace Rudoger.Modules.Product.Domain;

public sealed class ProductPackaging
{
    public required Guid Id { get; init; }

    public required int Level { get; init; }

    public required string UomCode { get; init; }

    public required decimal ConversionFactor { get; init; }

    public string? Barcode { get; init; }

    public decimal? WeightInKg { get; init; }

    public decimal? LengthInMm { get; init; }

    public decimal? WidthInMm { get; init; }

    public decimal? HeightInMm { get; init; }
}
