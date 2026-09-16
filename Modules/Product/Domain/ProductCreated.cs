using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Product.Domain;

public sealed record ProductCreated(
    Guid ProductId,
    string Sku,
    string Name,
    string BaseUomCode,
    decimal BasePriceAmount,
    string BasePriceCurrencyCode) : IDomainEvent;
