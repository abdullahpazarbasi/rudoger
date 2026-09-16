namespace Rudoger.Modules.Order.Presentation;

public interface IOrderInternalApi
{
    Task<bool> HasAnyOrderAsync(Guid productId, CancellationToken cancellationToken);
}
