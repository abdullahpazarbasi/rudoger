namespace Rudoger.Modules.Order.Infrastructure;

public sealed class OrderPlacementLineReadEntity
{
    public Guid Id { get; set; }

    public Guid PlacementId { get; set; }

    public int Num { get; set; }

    public Guid ProductId { get; set; }

    public string UomCode { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public Guid ReservationOperationId { get; set; }

    public Guid ReleaseOperationId { get; set; }

    public OrderPlacementReadEntity Placement { get; set; } = null!;
}
