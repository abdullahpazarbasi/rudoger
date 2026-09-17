namespace Rudoger.Modules.Inventory.Application;

public interface IProductInventoryGateway
{
    Task<InventoryProductOffer> ClaimOfferAsync(
        Guid productId,
        Guid operationId,
        string uomCode,
        CancellationToken cancellationToken);

    Task ReleaseUsageAsync(Guid productId, Guid operationId, CancellationToken cancellationToken);
}
