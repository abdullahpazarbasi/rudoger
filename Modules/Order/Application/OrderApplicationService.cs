using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Application;

public sealed class OrderApplicationService(
    IOrderRepository orderRepository,
    IOrderPlacementRepository placementRepository,
    ICorrelationContextAccessor correlationContextAccessor)
{
    public async Task<OrderPlacementView> CreatePlacementAsync(
        CreateOrderPlacementCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        OrderPlacementView? existing = await placementRepository.GetByIdempotencyKeyAsync(
            command.IdempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            EnsureSamePlacement(existing, command);
            return existing;
        }

        Guid userId = correlationContextAccessor.Current.UserId
            ?? throw new UnauthorizedAccessException("The authenticated user id is unavailable.");
        OrderPlacementLine[] lines = command.Lines.Select((line, index) => new OrderPlacementLine(
            Guid.CreateVersion7(),
            index + 1,
            line.ProductId,
            line.UomCode.Trim().ToUpperInvariant(),
            line.Quantity,
            Guid.CreateVersion7(),
            Guid.CreateVersion7())).ToArray();
        var aggregate = OrderPlacementAggregate.Request(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            userId,
            command.IdempotencyKey,
            lines);
        await placementRepository.SaveAsync(aggregate, cancellationToken);
        return await GetPlacementAsync(aggregate.Id, cancellationToken);
    }

    public async Task<OrderPlacementView> GetPlacementAsync(Guid placementId, CancellationToken cancellationToken)
    {
        return await placementRepository.GetAsync(placementId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order placement '{placementId}' was not found.");
    }

    public async Task<OrderView> GetAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await orderRepository.GetAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order '{orderId}' was not found.");
    }

    public Task<Page<OrderView>> ListAsync(int? pageNumber, int? pageSize, CancellationToken cancellationToken)
    {
        (int number, int size) = Paging.Normalize(pageNumber, pageSize);
        return orderRepository.ListAsync(number, size, cancellationToken);
    }

    public async Task<OrderTransitionView> RequestTransitionAsync(
        Guid orderId,
        OrderTransitionTarget target,
        CancellationToken cancellationToken)
    {
        OrderAggregate aggregate = await orderRepository.LoadAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order '{orderId}' was not found.");
        Guid transitionId = aggregate.RequestTransition(target);
        await orderRepository.SaveAsync(aggregate, cancellationToken);
        return await GetTransitionAsync(orderId, transitionId, cancellationToken);
    }

    public async Task<OrderTransitionView> GetTransitionAsync(
        Guid orderId,
        Guid transitionId,
        CancellationToken cancellationToken)
    {
        return await orderRepository.GetTransitionAsync(orderId, transitionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Order transition '{transitionId}' was not found.");
    }

    public Task<bool> HasAnyOrderAsync(Guid productId, CancellationToken cancellationToken)
    {
        return orderRepository.HasAnyOrderAsync(productId, cancellationToken);
    }

    private static void EnsureSamePlacement(OrderPlacementView existing, CreateOrderPlacementCommand command)
    {
        bool same = existing.Lines.Count == command.Lines.Count
            && existing.Lines.Zip(command.Lines).All(pair =>
                pair.First.ProductId == pair.Second.ProductId
                && string.Equals(pair.First.UomCode, pair.Second.UomCode.Trim(), StringComparison.OrdinalIgnoreCase)
                && pair.First.Quantity == pair.Second.Quantity);
        if (!same)
        {
            throw new ConflictException(
                "idempotency-key-conflict",
                "The idempotency key was already used for a different order placement request.");
        }
    }
}
