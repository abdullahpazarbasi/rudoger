using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.BuildingBlocks.Application;

public interface IEventRepository<TAggregate>
    where TAggregate : AggregateRoot
{
    Task<TAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken);

    Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken);
}
