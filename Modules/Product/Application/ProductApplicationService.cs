using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Product.Domain;

namespace Rudoger.Modules.Product.Application;

public sealed class ProductApplicationService(
    IProductRepository repository,
    IInventoryProductUsageGateway inventoryGateway,
    IOrderProductUsageGateway orderGateway)
{
    public async Task<ProductView> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        string normalizedSku = command.Sku.Trim().ToUpperInvariant();
        if (await repository.IsSkuInUseAsync(normalizedSku, null, cancellationToken))
        {
            throw new ConflictException("product-sku-conflict", $"SKU '{normalizedSku}' is already in use.");
        }

        PackagingDefinition[] packagings = command.Packagings
            .Select(ToDefinition)
            .ToArray();
        await EnsureBarcodesAvailableAsync(packagings, null, cancellationToken);

        var aggregate = ProductAggregate.Create(
            Guid.CreateVersion7(),
            command.Sku,
            command.Name,
            command.BaseUomCode,
            command.BasePriceAmount,
            command.BasePriceCurrencyCode,
            packagings);

        await repository.SaveAsync(aggregate, cancellationToken);
        return await GetRequiredAsync(aggregate.Id, cancellationToken);
    }

    public Task<Page<ProductView>> ListAsync(
        IReadOnlyCollection<Guid>? ids,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        (int number, int size) = Paging.Normalize(pageNumber, pageSize);
        return repository.ListAsync(ids ?? [], number, size, cancellationToken);
    }

    public Task<ProductView> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetRequiredAsync(id, cancellationToken);
    }

    public async Task<ProductView> ChangeAsync(Guid id, ChangeProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ProductAggregate aggregate = await LoadRequiredAsync(id, cancellationToken);
        string normalizedSku = command.Sku.Trim().ToUpperInvariant();
        if (await repository.IsSkuInUseAsync(normalizedSku, id, cancellationToken))
        {
            throw new ConflictException("product-sku-conflict", $"SKU '{normalizedSku}' is already in use.");
        }

        aggregate.Change(command.Sku, command.Name, command.BasePriceAmount, command.BasePriceCurrencyCode);
        await repository.SaveAsync(aggregate, cancellationToken);
        return await GetRequiredAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        ProductAggregate aggregate = await LoadRequiredAsync(id, cancellationToken);
        if (await inventoryGateway.HasAnyStockAsync(id, cancellationToken))
        {
            throw new ConflictException("product-has-stock", "A product with stock cannot be deleted.");
        }

        if (await orderGateway.HasAnyOrderAsync(id, cancellationToken))
        {
            throw new ConflictException("product-has-orders", "A product referenced by an order cannot be deleted.");
        }

        aggregate.Delete();
        await repository.SaveAsync(aggregate, cancellationToken);
    }

    public async Task<ProductPackagingView> AddPackagingAsync(
        Guid productId,
        PackagingInput input,
        CancellationToken cancellationToken)
    {
        ProductAggregate aggregate = await LoadRequiredAsync(productId, cancellationToken);
        PackagingDefinition definition = ToDefinition(input);
        await EnsureBarcodeAvailableAsync(definition.Barcode, null, cancellationToken);
        aggregate.AddPackaging(definition);
        await repository.SaveAsync(aggregate, cancellationToken);
        return ToView(aggregate.Packagings.Single(item => item.Id == definition.Id));
    }

    public async Task<ProductPackagingView> ChangePackagingAsync(
        Guid productId,
        Guid packagingId,
        ChangePackagingCommand command,
        CancellationToken cancellationToken)
    {
        ProductAggregate aggregate = await LoadRequiredAsync(productId, cancellationToken);
        var definition = new PackagingDefinition(
            packagingId,
            command.Level,
            command.UomCode,
            command.ConversionFactor,
            command.Barcode,
            command.WeightInKg,
            command.LengthInMm,
            command.WidthInMm,
            command.HeightInMm);
        await EnsureBarcodeAvailableAsync(definition.Barcode, packagingId, cancellationToken);
        aggregate.ChangePackaging(definition);
        await repository.SaveAsync(aggregate, cancellationToken);
        return ToView(aggregate.Packagings.Single(item => item.Id == packagingId));
    }

    public async Task RemovePackagingAsync(Guid productId, Guid packagingId, CancellationToken cancellationToken)
    {
        ProductAggregate aggregate = await LoadRequiredAsync(productId, cancellationToken);
        aggregate.RemovePackaging(packagingId);
        await repository.SaveAsync(aggregate, cancellationToken);
    }

    private async Task EnsureBarcodesAvailableAsync(
        IEnumerable<PackagingDefinition> packagings,
        Guid? excludedPackagingId,
        CancellationToken cancellationToken)
    {
        foreach (string barcode in packagings.Where(item => !string.IsNullOrWhiteSpace(item.Barcode)).Select(item => item.Barcode!))
        {
            await EnsureBarcodeAvailableAsync(barcode, excludedPackagingId, cancellationToken);
        }
    }

    private async Task EnsureBarcodeAvailableAsync(string? barcode, Guid? excludedPackagingId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(barcode)
            && await repository.IsBarcodeInUseAsync(barcode.Trim(), excludedPackagingId, cancellationToken))
        {
            throw new ConflictException("product-barcode-conflict", $"Barcode '{barcode.Trim()}' is already in use.");
        }
    }

    private async Task<ProductAggregate> LoadRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        ProductAggregate? aggregate = await repository.LoadAsync(id, cancellationToken);
        if (aggregate is null || aggregate.IsDeleted)
        {
            throw new NotFoundException(ProductFailureCode.ProductNotFound, $"Product '{id}' was not found.");
        }

        return aggregate;
    }

    private async Task<ProductView> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await repository.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException(ProductFailureCode.ProductNotFound, $"Product '{id}' was not found.");
    }

    private static PackagingDefinition ToDefinition(PackagingInput input)
    {
        return new PackagingDefinition(
            input.Id ?? Guid.CreateVersion7(),
            input.Level,
            input.UomCode,
            input.ConversionFactor,
            input.Barcode,
            input.WeightInKg,
            input.LengthInMm,
            input.WidthInMm,
            input.HeightInMm);
    }

    private static ProductPackagingView ToView(ProductPackaging packaging)
    {
        return new ProductPackagingView(
            packaging.Id,
            packaging.Level,
            packaging.UomCode,
            packaging.ConversionFactor,
            packaging.Barcode,
            packaging.WeightInKg,
            packaging.LengthInMm,
            packaging.WidthInMm,
            packaging.HeightInMm);
    }
}
