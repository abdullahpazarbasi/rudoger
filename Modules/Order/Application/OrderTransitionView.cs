using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Application;

public sealed record OrderTransitionView(
    Guid Id,
    Guid OrderId,
    OrderTransitionTarget Target,
    OrderTransitionStatus Status);
