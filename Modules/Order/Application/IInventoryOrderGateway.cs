namespace Rudoger.Modules.Order.Application;

public interface IInventoryOrderGateway
{
    Task ReserveAsync(
        Guid productId,
        decimal quantity,
        Guid orderId,
        Guid sourceEventId,
        CancellationToken cancellationToken);

    Task CommitAsync(Guid productId, Guid orderId, Guid sourceEventId, CancellationToken cancellationToken);

    Task ReleaseAsync(Guid productId, Guid orderId, Guid sourceEventId, CancellationToken cancellationToken);

    Task CompensateReservationAsync(
        Guid productId,
        Guid orderId,
        Guid sourceEventId,
        CancellationToken cancellationToken);
}
