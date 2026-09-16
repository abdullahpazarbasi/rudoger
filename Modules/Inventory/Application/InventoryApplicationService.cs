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
        StockItemCreationView? existingByKey = await repository.GetByCreationIdempotencyKeyAsync(
            command.IdempotencyKey,
            cancellationToken);
        if (existingByKey is not null)
        {
            if (existingByKey.StockItem.ProductId != command.ProductId
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
        string baseUomCode = await productGateway.ClaimBaseUomAsync(command.ProductId, operationId, cancellationToken);
        try
        {
            var aggregate = StockItemAggregate.Open(
                Guid.CreateVersion7(),
                command.ProductId,
                baseUomCode,
                command.OpeningQuantity,
                command.IdempotencyKey,
                correlationContextAccessor.Current.CorrelationId,
                operationId);
            await repository.SaveAsync(aggregate, cancellationToken);
            return await GetRequiredAsync(aggregate.Id, cancellationToken);
        }
        catch
        {
            await productGateway.ReleaseUsageAsync(command.ProductId, operationId, CancellationToken.None);
            throw;
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
        StockMovementView? existing = await repository.GetMovementByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            EnsureSameRequest(existing, stockItemId, command);
            return existing;
        }

        StockItemAggregate aggregate = await LoadRequiredAsync(stockItemId, cancellationToken);
        Guid operationId = Guid.CreateVersion7();
        await productGateway.ClaimBaseUomAsync(aggregate.ProductId, operationId, cancellationToken);
        try
        {
            string correlationId = correlationContextAccessor.Current.CorrelationId;
            switch (command.Type)
            {
                case StockMovementType.Receipt:
                    aggregate.Receive(command.Quantity, command.IdempotencyKey, correlationId, operationId);
                    break;
                case StockMovementType.Adjustment:
                    aggregate.Adjust(command.Quantity, command.IdempotencyKey, correlationId, operationId);
                    break;
                case StockMovementType.Deduction:
                    aggregate.Deduct(command.Quantity, command.IdempotencyKey, correlationId, operationId);
                    break;
                default:
                    throw new DomainException(
                        "stock-movement-type-forbidden",
                        "Reserved, committed, and released movements are available only to the Order internal API.");
            }

            await repository.SaveAsync(aggregate, cancellationToken);
            return await repository.GetMovementByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("The projected stock movement was not found.");
        }
        catch
        {
            await productGateway.ReleaseUsageAsync(aggregate.ProductId, operationId, CancellationToken.None);
            throw;
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
            ?? throw new KeyNotFoundException($"Stock item '{id}' was not found.");
    }

    private async Task<StockItemView> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock item '{id}' was not found.");
    }

    private static void EnsureSameRequest(
        StockMovementView existing,
        Guid stockItemId,
        CreateStockMovementCommand command)
    {
        decimal expectedOnHandDelta = command.Type switch
        {
            StockMovementType.Receipt => command.Quantity,
            StockMovementType.Adjustment => command.Quantity,
            StockMovementType.Deduction => -command.Quantity,
            _ => decimal.MinValue,
        };
        if (existing.StockItemId != stockItemId
            || existing.Type != command.Type
            || existing.OnHandQuantityDelta != expectedOnHandDelta)
        {
            throw new ConflictException(
                "idempotency-key-conflict",
                "The idempotency key was already used for a different stock movement request.");
        }
    }
}
