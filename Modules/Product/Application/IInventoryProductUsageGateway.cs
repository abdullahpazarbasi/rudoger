namespace Rudoger.Modules.Product.Application;

public interface IInventoryProductUsageGateway
{
    Task<bool> HasAnyStockAsync(Guid productId, CancellationToken cancellationToken);
}
