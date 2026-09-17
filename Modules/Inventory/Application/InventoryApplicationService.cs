using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.Modules.Inventory.Application;

public sealed class InventoryApplicationService(
    IInventoryRepository repository,
    IProductInventoryGateway productGateway,
    ICorrelationContextAccessor correlationContextAccessor)
{
    public async Task<StockItemView> CreateAsync(CreateStockItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        string uomCode = NormalizeUomCode(command.UomCode);
        StockItemCreationView? existingByKey = await repository.GetByCreationIdempotencyKeyAsync(
            command.IdempotencyKey,
            cancellationToken);
        if (existingByKey is not null)
        {
            if (existingByKey.StockItem.ProductId != command.ProductId
                || !string.Equals(existingByKey.UomCode, uomCode, StringComparison.Ordinal)
                || existingByKey.OpeningQuantity != command.OpeningQuantity)
            {
                throw new ConflictException(
                    "idempotency-key-conflict",
                    "The idempotency key was already used for a different stock item request.");
            }

            return existingByKey.StockItem;
        }

        if (await repository.GetByProductAsync(command.ProductId, cancellationToken) is not null)
        {
            throw new ConflictException("stock-item-already-exists", $"A stock item already exists for product '{command.ProductId}'.");
        }

        Guid operationId = Guid.CreateVersion7();
        InventoryProductOffer offer = await productGateway.ClaimOfferAsync(
            command.ProductId,
            operationId,
            uomCode,
            cancellationToken);
        bool releaseUsageSynchronously = true;
        try
        {
            decimal baseOpeningQuantity = offer.ToBaseQuantity(command.OpeningQuantity);
            var aggregate = StockItemAggregate.Open(
                Guid.CreateVersion7(),
                command.ProductId,
                offer.BaseUomCode,
                baseOpeningQuantity,
                command.IdempotencyKey,
                correlationContextAccessor.Current.CorrelationId,
                operationId,
                uomCode,
                command.OpeningQuantity);
            await repository.SaveAsync(aggregate, cancellationToken);
            releaseUsageSynchronously = false;
            return await GetRequiredAsync(aggregate.Id, cancellationToken);
        }
        finally
        {
            if (releaseUsageSynchronously)
            {
                await productGateway.ReleaseUsageAsync(command.ProductId, operationId, CancellationToken.None);
            }
        }
    }

    public Task<Page<StockItemView>> ListAsync(
        Guid? productId,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        (int number, int size) = Paging.Normalize(pageNumber, pageSize);
        return repository.ListAsync(productId, number, size, cancellationToken);
    }

    public Task<StockItemView> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetRequiredAsync(id, cancellationToken);
    }

    public async Task<StockMovementView> MoveAsync(
        Guid stockItemId,
        CreateStockMovementCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureManualMovementType(command.Type);
        string uomCode = NormalizeUomCode(command.UomCode);
        StockMovementView? existing = await repository.GetMovementByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            EnsureSameRequest(existing, stockItemId, command, uomCode);
            return existing;
        }

        StockItemAggregate aggregate = await LoadRequiredAsync(stockItemId, cancellationToken);
        Guid operationId = Guid.CreateVersion7();
        InventoryProductOffer offer = await productGateway.ClaimOfferAsync(
            aggregate.ProductId,
            operationId,
            uomCode,
            cancellationToken);
        bool releaseUsageSynchronously = true;
        try
        {
            if (!string.Equals(offer.BaseUomCode, aggregate.BaseUomCode, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Product '{aggregate.ProductId}' returned base UoM '{offer.BaseUomCode}' "
                    + $"for stock item base UoM '{aggregate.BaseUomCode}'.");
            }

            decimal baseQuantity = offer.ToBaseQuantity(command.Quantity);
            string correlationId = correlationContextAccessor.Current.CorrelationId;
            switch (command.Type)
            {
                case StockMovementType.Receipt:
                    aggregate.Receive(
                        baseQuantity,
                        command.IdempotencyKey,
                        correlationId,
                        operationId,
                        uomCode,
                        command.Quantity);
                    break;
                case StockMovementType.Adjustment:
                    aggregate.Adjust(
                        baseQuantity,
                        command.IdempotencyKey,
                        correlationId,
                        operationId,
                        uomCode,
                        command.Quantity);
                    break;
                case StockMovementType.Deduction:
                    aggregate.Deduct(
                        baseQuantity,
                        command.IdempotencyKey,
                        correlationId,
                        operationId,
                        uomCode,
                        command.Quantity);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported manual movement type '{command.Type}'.");
            }

            await repository.SaveAsync(aggregate, cancellationToken);
            releaseUsageSynchronously = false;
            return await repository.GetMovementByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("The stock movement was not found after it was saved.");
        }
        finally
        {
            if (releaseUsageSynchronously)
            {
                await productGateway.ReleaseUsageAsync(aggregate.ProductId, operationId, CancellationToken.None);
            }
        }
    }

    public Task<Page<StockMovementView>> ListMovementsAsync(
        Guid stockItemId,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        (int number, int size) = Paging.Normalize(pageNumber, pageSize);
        return repository.ListMovementsAsync(stockItemId, number, size, cancellationToken);
    }

    private async Task<StockItemAggregate> LoadRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await repository.LoadAsync(id, cancellationToken)
            ?? throw new NotFoundException(InventoryFailureCode.StockItemNotFound, $"Stock item '{id}' was not found.");
    }

    private async Task<StockItemView> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await repository.GetAsync(id, cancellationToken)
            ?? throw new NotFoundException(InventoryFailureCode.StockItemNotFound, $"Stock item '{id}' was not found.");
    }

    private static void EnsureSameRequest(
        StockMovementView existing,
        Guid stockItemId,
        CreateStockMovementCommand command,
        string uomCode)
    {
        if (existing.StockItemId != stockItemId
            || existing.Type != command.Type
            || !string.Equals(existing.UomCode, uomCode, StringComparison.Ordinal)
            || existing.Quantity != command.Quantity)
        {
            throw new ConflictException(
                "idempotency-key-conflict",
                "The idempotency key was already used for a different stock movement request.");
        }
    }

    private static void EnsureManualMovementType(StockMovementType type)
    {
        if (type is not (StockMovementType.Receipt or StockMovementType.Adjustment or StockMovementType.Deduction))
        {
            throw new DomainException(
                "stock-movement-type-forbidden",
                "Reserved, committed, and released movements are available only to the Order internal API.");
        }
    }

    private static string NormalizeUomCode(string uomCode)
    {
        return Guard.Required(
            uomCode,
            "stock-item-uom-invalid",
            "UoM code",
            InventoryRules.UomCodeMaximumLength).ToUpperInvariant();
    }
}
