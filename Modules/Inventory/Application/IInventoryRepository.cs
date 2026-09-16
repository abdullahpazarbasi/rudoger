using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Application;

public interface IInventoryRepository : IEventRepository<StockItemAggregate>
{
    Task<StockItemView?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<StockItemAggregate?> LoadByProductAsync(Guid productId, CancellationToken cancellationToken);

    Task<StockItemView?> GetByProductAsync(Guid productId, CancellationToken cancellationToken);

    Task<StockItemCreationView?> GetByCreationIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<Page<StockItemView>> ListAsync(Guid? productId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<StockMovementView?> GetMovementByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<Page<StockMovementView>> ListMovementsAsync(
        Guid stockItemId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
