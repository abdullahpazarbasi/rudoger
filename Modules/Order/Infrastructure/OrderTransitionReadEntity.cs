using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Infrastructure;

public sealed class OrderTransitionReadEntity
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public OrderTransitionTarget Target { get; set; }

    public OrderTransitionStatus Status { get; set; }
}
