using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Application;

public interface IOrderPlacementRepository : IEventRepository<OrderPlacementAggregate>
{
    Task<OrderPlacementView?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<OrderPlacementView?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
}
