using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Presentation;

public sealed record CreateOrderTransitionRequest(OrderTransitionTarget Target);
