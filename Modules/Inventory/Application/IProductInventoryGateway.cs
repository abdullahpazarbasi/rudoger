namespace Rudoger.Modules.Inventory.Application;

public interface IProductInventoryGateway
{
    Task<string> ClaimBaseUomAsync(Guid productId, Guid operationId, CancellationToken cancellationToken);

    Task ReleaseUsageAsync(Guid productId, Guid operationId, CancellationToken cancellationToken);
}
