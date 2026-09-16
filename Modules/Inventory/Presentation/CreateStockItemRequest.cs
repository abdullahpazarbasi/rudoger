namespace Rudoger.Modules.Inventory.Presentation;

public sealed record CreateStockItemRequest(Guid ProductId, decimal OpeningQuantity);
