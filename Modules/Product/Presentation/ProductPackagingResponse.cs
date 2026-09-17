using Rudoger.Modules.Product.Application;

namespace Rudoger.Modules.Product.Presentation;

public sealed record ProductPackagingResponse(
    Guid Id,
    int Level,
    string UomCode,
    decimal ConversionFactor,
    string? Barcode,
    decimal? WeightInKg,
    decimal? LengthInMm,
    decimal? WidthInMm,
    decimal? HeightInMm)
{
    public static ProductPackagingResponse From(ProductPackagingView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new ProductPackagingResponse(
            view.Id,
            view.Level,
            view.UomCode,
            view.ConversionFactor,
            view.Barcode,
            view.WeightInKg,
            view.LengthInMm,
            view.WidthInMm,
            view.HeightInMm);
    }
}
