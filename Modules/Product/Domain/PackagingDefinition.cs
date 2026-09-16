namespace Rudoger.Modules.Product.Domain;

public sealed record PackagingDefinition(
    Guid Id,
    int Level,
    string UomCode,
    decimal ConversionFactor,
    string? Barcode,
    decimal? WeightInKg,
    decimal? LengthInMm,
    decimal? WidthInMm,
    decimal? HeightInMm);
