using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Product.Domain;

public sealed record ProductChanged(
    string Sku,
    string Name,
    decimal BasePriceAmount,
    string BasePriceCurrencyCode) : IDomainEvent;
