namespace Rudoger.Modules.Product.Application;

public sealed record PackagingInput(
    Guid? Id,
    int Level,
    string UomCode,
    decimal ConversionFactor,
    string? Barcode,
    decimal? WeightInKg,
    decimal? LengthInMm,
    decimal? WidthInMm,
    decimal? HeightInMm);
