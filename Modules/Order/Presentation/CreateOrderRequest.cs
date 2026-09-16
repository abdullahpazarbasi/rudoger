namespace Rudoger.Modules.Order.Presentation;

public sealed record CreateOrderRequest(IReadOnlyList<OrderPlacementLineRequest> Lines);
