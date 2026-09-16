namespace Rudoger.Modules.Order.Presentation;

public sealed record OrderPlacementLineRequest(Guid ProductId, string UomCode, decimal Quantity);
