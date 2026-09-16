using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Infrastructure;

public sealed class OrderReadEntity
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public Guid UserId { get; set; }

    public Guid? PendingTransitionId { get; set; }

    public OrderTransitionTarget? PendingTransitionTarget { get; set; }

    public List<OrderLineReadEntity> Lines { get; set; } = [];
}
