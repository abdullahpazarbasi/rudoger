using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Product.Domain;

namespace Rudoger.Modules.Product.Application;

public interface IProductRepository : IEventRepository<ProductAggregate>
{
    Task<ProductView?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<Page<ProductView>> ListAsync(
        IReadOnlyCollection<Guid> ids,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> IsSkuInUseAsync(string sku, Guid? excludedProductId, CancellationToken cancellationToken);

    Task<bool> IsBarcodeInUseAsync(string barcode, Guid? excludedPackagingId, CancellationToken cancellationToken);
}
