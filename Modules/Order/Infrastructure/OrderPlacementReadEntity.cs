using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Infrastructure;

public sealed class OrderPlacementReadEntity
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public OrderPlacementStatus Status { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureDetail { get; set; }

    public List<OrderPlacementLineReadEntity> Lines { get; set; } = [];
}
