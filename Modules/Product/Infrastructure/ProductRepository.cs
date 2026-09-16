using Microsoft.EntityFrameworkCore;
using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Product.Application;
using Rudoger.Modules.Product.Domain;

namespace Rudoger.Modules.Product.Infrastructure;

public sealed class ProductRepository(ProductDbContext dbContext, EventStore<ProductDbContext> eventStore)
    : IProductRepository
{
    private const string AggregateType = "product";

    public async Task<ProductAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        IReadOnlyList<IDomainEvent> events = await eventStore.LoadAsync(id, cancellationToken);
        if (events.Count == 0)
        {
            return null;
        }

        var aggregate = new ProductAggregate();
        aggregate.LoadFromHistory(events);
        return aggregate;
    }

    public Task SaveAsync(ProductAggregate aggregate, CancellationToken cancellationToken)
    {
        CurrentStreamId = aggregate.Id;
        return eventStore.AppendAsync(AggregateType, aggregate, ProjectAsync, cancellationToken);
    }

    public async Task<ProductView?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        ProductReadEntity? entity = await dbContext.Products
            .AsNoTracking()
            .Include(item => item.Packagings)
            .SingleOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        return entity is null ? null : ToView(entity);
    }

    public async Task<Page<ProductView>> ListAsync(
        IReadOnlyCollection<Guid> ids,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductReadEntity> query = dbContext.Products
            .AsNoTracking()
            .Include(item => item.Packagings)
            .Where(item => !item.IsDeleted);
        if (ids.Count > 0)
        {
            query = query.Where(item => ids.Contains(item.Id));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        List<ProductReadEntity> entities = await query
            .OrderBy(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Page<ProductView>(entities.Select(ToView).ToArray(), pageNumber, pageSize, totalCount);
    }

    public Task<bool> IsSkuInUseAsync(string sku, Guid? excludedProductId, CancellationToken cancellationToken)
    {
        return dbContext.Products.AsNoTracking().AnyAsync(
            item => item.Sku == sku && (!excludedProductId.HasValue || item.Id != excludedProductId.Value),
            cancellationToken);
    }

    public Task<bool> IsBarcodeInUseAsync(string barcode, Guid? excludedPackagingId, CancellationToken cancellationToken)
    {
        return dbContext.ProductPackagings.AsNoTracking().AnyAsync(
            item => item.Barcode == barcode && (!excludedPackagingId.HasValue || item.Id != excludedPackagingId.Value),
            cancellationToken);
    }

    private Task ProjectAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return domainEvent switch
        {
            ProductCreated created => ProjectCreatedAsync(created, cancellationToken),
            ProductChanged changed => ProjectChangedAsync(changed, cancellationToken),
            ProductPackagingAdded added => ProjectPackagingAddedAsync(added, cancellationToken),
            ProductPackagingChanged changed => ProjectPackagingChangedAsync(changed, cancellationToken),
            ProductPackagingRemoved removed => ProjectPackagingRemovedAsync(removed, cancellationToken),
            ProductUsageClaimed claimed => ProjectUsageClaimedAsync(claimed),
            ProductUsageReleased released => ProjectUsageReleasedAsync(released, cancellationToken),
            ProductDeleted => ProjectDeletedAsync(cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported product event '{domainEvent.GetType().Name}'."),
        };
    }

    private Task ProjectCreatedAsync(ProductCreated created, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Products.Add(new ProductReadEntity
        {
            Id = created.ProductId,
            Sku = created.Sku,
            Name = created.Name,
            BaseUomCode = created.BaseUomCode,
            BasePriceAmount = created.BasePriceAmount,
            BasePriceCurrencyCode = created.BasePriceCurrencyCode,
        });
        return Task.CompletedTask;
    }

    private async Task ProjectChangedAsync(ProductChanged changed, CancellationToken cancellationToken)
    {
        ProductReadEntity product = await FindProductAsync(cancellationToken);
        product.Sku = changed.Sku;
        product.Name = changed.Name;
        product.BasePriceAmount = changed.BasePriceAmount;
        product.BasePriceCurrencyCode = changed.BasePriceCurrencyCode;
    }

    private Task ProjectPackagingAddedAsync(ProductPackagingAdded added, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProductPackagingReadEntity entity = ToEntity(added.Packaging);
        entity.ProductId = CurrentStreamId;
        dbContext.ProductPackagings.Add(entity);
        return Task.CompletedTask;
    }

    private async Task ProjectPackagingChangedAsync(ProductPackagingChanged changed, CancellationToken cancellationToken)
    {
        ProductPackagingReadEntity entity = await dbContext.ProductPackagings.SingleAsync(
            item => item.Id == changed.Packaging.Id,
            cancellationToken);
        Apply(entity, changed.Packaging);
    }

    private async Task ProjectPackagingRemovedAsync(ProductPackagingRemoved removed, CancellationToken cancellationToken)
    {
        ProductPackagingReadEntity entity = await dbContext.ProductPackagings.SingleAsync(
            item => item.Id == removed.PackagingId,
            cancellationToken);
        dbContext.ProductPackagings.Remove(entity);
    }

    private Task ProjectUsageClaimedAsync(ProductUsageClaimed claimed)
    {
        dbContext.ProductUsageClaims.Add(new ProductUsageClaimEntity
        {
            ProductId = CurrentStreamId,
            OperationId = claimed.OperationId,
            UsageType = claimed.UsageType,
        });
        return Task.CompletedTask;
    }

    private async Task ProjectUsageReleasedAsync(ProductUsageReleased released, CancellationToken cancellationToken)
    {
        ProductUsageClaimEntity? entity = await dbContext.ProductUsageClaims.SingleOrDefaultAsync(
            item => item.ProductId == CurrentStreamId && item.OperationId == released.OperationId,
            cancellationToken);
        if (entity is not null)
        {
            dbContext.ProductUsageClaims.Remove(entity);
        }
    }

    private async Task ProjectDeletedAsync(CancellationToken cancellationToken)
    {
        ProductReadEntity product = await FindProductAsync(cancellationToken);
        product.IsDeleted = true;
    }

    private Guid CurrentStreamId { get; set; }

    private async Task<ProductReadEntity> FindProductAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Products.SingleAsync(item => item.Id == CurrentStreamId, cancellationToken);
    }

    private static ProductPackagingReadEntity ToEntity(PackagingDefinition definition)
    {
        var entity = new ProductPackagingReadEntity
        {
            Id = definition.Id,
        };
        Apply(entity, definition);
        return entity;
    }

    private static void Apply(ProductPackagingReadEntity entity, PackagingDefinition definition)
    {
        entity.Level = definition.Level;
        entity.UomCode = definition.UomCode;
        entity.ConversionFactor = definition.ConversionFactor;
        entity.Barcode = definition.Barcode;
        entity.WeightInKg = definition.WeightInKg;
        entity.LengthInMm = definition.LengthInMm;
        entity.WidthInMm = definition.WidthInMm;
        entity.HeightInMm = definition.HeightInMm;
    }

    private static ProductView ToView(ProductReadEntity entity)
    {
        return new ProductView(
            entity.Id,
            entity.Sku,
            entity.Name,
            entity.BaseUomCode,
            entity.BasePriceAmount,
            entity.BasePriceCurrencyCode,
            entity.Packagings.OrderBy(item => item.Level).Select(ToView).ToArray());
    }

    private static ProductPackagingView ToView(ProductPackagingReadEntity entity)
    {
        return new ProductPackagingView(
            entity.Id,
            entity.Level,
            entity.UomCode,
            entity.ConversionFactor,
            entity.Barcode,
            entity.WeightInKg,
            entity.LengthInMm,
            entity.WidthInMm,
            entity.HeightInMm);
    }

}
