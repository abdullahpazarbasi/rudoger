namespace Rudoger.Modules.Product.Application;

public interface IOrderProductUsageGateway
{
    Task<bool> HasAnyOrderAsync(Guid productId, CancellationToken cancellationToken);
}
