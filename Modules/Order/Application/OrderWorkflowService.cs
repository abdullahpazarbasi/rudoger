using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Order.Domain;

namespace Rudoger.Modules.Order.Application;

public sealed class OrderWorkflowService(
    IOrderRepository orderRepository,
    IOrderPlacementRepository placementRepository,
    IProductOrderGateway productGateway,
    IInventoryOrderGateway inventoryGateway)
{
    public async Task ProcessPlacementAsync(Guid placementId, CancellationToken cancellationToken)
    {
        OrderPlacementAggregate placement = await placementRepository.LoadAsync(placementId, cancellationToken)
            ?? throw new NotFoundException(
                "order-placement-not-found",
                $"Order placement '{placementId}' was not found.");
        if (placement.Status != OrderPlacementStatus.Pending)
        {
            await ReleaseAllProductUsageAsync(placement, cancellationToken);
            return;
        }

        IGrouping<Guid, OrderPlacementLine>[] groups = placement.Lines
            .GroupBy(item => item.ProductId)
            .OrderBy(group => group.Key)
            .ToArray();
        var offers = new Dictionary<Guid, OrderProductOffer>();

        try
        {
            foreach (IGrouping<Guid, OrderPlacementLine> group in groups)
            {
                offers[group.Key] = await productGateway.ClaimOfferAsync(
                    group.Key,
                    placement.Id,
                    group.Select(item => item.UomCode).ToArray(),
                    cancellationToken);
            }

            EnsureSingleCurrency(offers.Values);
            OrderLineDefinition[] orderLines = placement.Lines
                .OrderBy(item => item.Num)
                .Select(line => ToOrderLine(line, offers[line.ProductId]))
                .ToArray();

            foreach (IGrouping<Guid, OrderPlacementLine> group in groups)
            {
                decimal baseQuantity = orderLines.Where(item => item.ProductId == group.Key).Sum(item => item.BaseQuantity);
                await inventoryGateway.ReserveAsync(
                    group.Key,
                    baseQuantity,
                    placement.OrderId,
                    group.First().ReservationOperationId,
                    cancellationToken);
            }

            if (await orderRepository.LoadAsync(placement.OrderId, cancellationToken) is null)
            {
                OrderAggregate order = OrderAggregate.Place(
                    placement.OrderId,
                    $"RDO-{placement.OrderId:N}",
                    placement.UserId,
                    orderLines);
                await orderRepository.SaveAsync(order, cancellationToken);
            }

            placement.Succeed();
            await placementRepository.SaveAsync(placement, cancellationToken);
        }
        catch (Exception exception) when (IsBusinessFailure(exception))
        {
            foreach (IGrouping<Guid, OrderPlacementLine> group in groups)
            {
                await inventoryGateway.CompensateReservationAsync(
                    group.Key,
                    placement.OrderId,
                    group.First().ReleaseOperationId,
                    cancellationToken);
            }

            (string code, string detail) = GetFailure(exception);
            placement.Fail(code, detail);
            await placementRepository.SaveAsync(placement, cancellationToken);
        }
        finally
        {
            foreach (Guid productId in offers.Keys)
            {
                await productGateway.ReleaseUsageAsync(productId, placement.Id, CancellationToken.None);
            }
        }
    }

    public async Task ProcessTransitionAsync(Guid orderId, Guid transitionId, CancellationToken cancellationToken)
    {
        OrderAggregate order = await orderRepository.LoadAsync(orderId, cancellationToken)
            ?? throw new NotFoundException("order-not-found", $"Order '{orderId}' was not found.");
        if (order.ActiveTransitionId != transitionId || !order.ActiveTransitionTarget.HasValue)
        {
            return;
        }

        foreach (TransitionStockOperation operation in order.ActiveStockOperations)
        {
            if (order.ActiveTransitionTarget == OrderTransitionTarget.Shipped)
            {
                await inventoryGateway.CommitAsync(
                    operation.ProductId,
                    order.Id,
                    operation.OperationId,
                    cancellationToken);
            }
            else
            {
                await inventoryGateway.ReleaseAsync(
                    operation.ProductId,
                    order.Id,
                    operation.OperationId,
                    cancellationToken);
            }
        }

        order.CompleteTransition(transitionId);
        await orderRepository.SaveAsync(order, cancellationToken);
    }

    private static OrderLineDefinition ToOrderLine(OrderPlacementLine line, OrderProductOffer offer)
    {
        decimal factor = offer.ConversionFactors[line.UomCode];
        return new OrderLineDefinition(
            line.Id,
            line.Num,
            line.ProductId,
            line.UomCode,
            line.Quantity,
            decimal.Round(offer.BasePriceAmount * factor, 4, MidpointRounding.ToEven),
            offer.CurrencyCode,
            line.Quantity * factor);
    }

    private static void EnsureSingleCurrency(IEnumerable<OrderProductOffer> offers)
    {
        if (offers.Select(item => item.CurrencyCode).Distinct(StringComparer.Ordinal).Count() != 1)
        {
            throw new DomainException("order-currency-mixed", "All products in an order must use the same currency.");
        }
    }

    // The gateways translate every product and inventory failure into the order vocabulary, so the
    // codes and details recorded here - and published by the order API - are always order-owned.
    private static bool IsBusinessFailure(Exception exception)
    {
        return exception is DomainException or ConflictException;
    }

    private static (string Code, string Detail) GetFailure(Exception exception)
    {
        return exception switch
        {
            DomainException domain => (domain.Code, domain.Message),
            ConflictException conflict => (conflict.Code, conflict.Message),
            _ => throw new InvalidOperationException("The exception is not a business failure.", exception),
        };
    }

    private async Task ReleaseAllProductUsageAsync(
        OrderPlacementAggregate placement,
        CancellationToken cancellationToken)
    {
        foreach (Guid productId in placement.Lines.Select(item => item.ProductId).Distinct())
        {
            await productGateway.ReleaseUsageAsync(productId, placement.Id, cancellationToken);
        }
    }
}
