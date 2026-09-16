namespace Rudoger.Modules.Order.Application;

public sealed record CreateOrderPlacementCommand(
    string IdempotencyKey,
    IReadOnlyList<OrderPlacementLineInput> Lines);
