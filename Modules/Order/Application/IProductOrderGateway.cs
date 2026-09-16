namespace Rudoger.Modules.Order.Application;

public interface IProductOrderGateway
{
    Task<OrderProductOffer> ClaimOfferAsync(
        Guid productId,
        Guid operationId,
        IReadOnlyCollection<string> uomCodes,
        CancellationToken cancellationToken);

    Task ReleaseUsageAsync(Guid productId, Guid operationId, CancellationToken cancellationToken);
}
