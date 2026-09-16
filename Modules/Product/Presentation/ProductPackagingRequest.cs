namespace Rudoger.Modules.Product.Presentation;

public sealed record ProductPackagingRequest(
    Guid? Id,
    int Level,
    string UomCode,
    decimal ConversionFactor,
    string? Barcode,
    decimal? WeightInKg,
    decimal? LengthInMm,
    decimal? WidthInMm,
    decimal? HeightInMm);
