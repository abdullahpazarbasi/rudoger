using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Application;

public interface IOrderRepository : IEventRepository<OrderAggregate>
{
    Task<OrderView?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<Page<OrderView>> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<bool> HasAnyOrderAsync(Guid productId, CancellationToken cancellationToken);

    Task<OrderTransitionView?> GetTransitionAsync(Guid orderId, Guid transitionId, CancellationToken cancellationToken);
}
