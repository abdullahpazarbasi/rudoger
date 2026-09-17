using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Inventory.Domain;

public sealed record StockItemOpened(
    Guid StockItemId,
    Guid ProductId,
    string BaseUomCode,
    decimal OpeningQuantity,
    string IdempotencyKey,
    Guid ProductUsageOperationId,
    string OpeningUomCode,
    decimal RequestedOpeningQuantity) : IDomainEvent;
