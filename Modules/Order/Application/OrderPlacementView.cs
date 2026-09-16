using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Application;

public sealed record OrderPlacementView(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    OrderPlacementStatus Status,
    IReadOnlyList<OrderPlacementLineView> Lines,
    string? FailureCode,
    string? FailureDetail);
