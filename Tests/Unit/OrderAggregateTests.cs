using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Order.Domain;

namespace Rudoger.UnitTests;

public sealed class OrderAggregateTests
{
    [Fact]
    public void PlaceCreatesPlacedOrderWithPriceSnapshots()
    {
        OrderAggregate order = CreateOrder();

        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(2, order.Lines.Count);
        Assert.IsType<OrderPlaced>(Assert.Single(order.UncommittedEvents));
    }

    [Fact]
    public void PlaceRejectsEmptyAndMixedCurrencyLines()
    {
        Assert.Throws<DomainException>(() => OrderAggregate.Place(
            Guid.CreateVersion7(), "RDO-1", Guid.CreateVersion7(), []));
        OrderLineDefinition[] lines = ValidLines();
        lines[1] = lines[1] with { UnitPriceCurrencyCode = "EUR" };
        Assert.Throws<DomainException>(() => OrderAggregate.Place(
            Guid.CreateVersion7(), "RDO-1", Guid.CreateVersion7(), lines));
    }

    [Fact]
    public void PlaceRejectsDuplicateProductUomPairs()
    {
        OrderLineDefinition[] lines = ValidLines();
        lines[1] = lines[1] with
        {
            ProductId = lines[0].ProductId,
            UomCode = lines[0].UomCode,
        };
        Assert.Throws<DomainException>(() => OrderAggregate.Place(
            Guid.CreateVersion7(), "RDO-1", Guid.CreateVersion7(), lines));
    }

    [Fact]
    public void RequestShipAndCompleteMovesOrderToShipped()
    {
        OrderAggregate order = CreateOrder();
        order.MarkChangesAsCommitted();

        Guid transitionId = order.RequestTransition(OrderTransitionTarget.Shipped);
        Assert.Equal(transitionId, order.ActiveTransitionId);
        Assert.Equal(2, order.ActiveStockOperations.Count);
        order.CompleteTransition(transitionId);

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Null(order.ActiveTransitionId);
    }

    [Fact]
    public void RequestCancelAndCompleteMovesOrderToCancelled()
    {
        OrderAggregate order = CreateOrder();
        Guid transitionId = order.RequestTransition(OrderTransitionTarget.Cancelled);
        order.CompleteTransition(transitionId);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void PendingOrTerminalOrderRejectsAnotherTransition()
    {
        OrderAggregate order = CreateOrder();
        Guid transitionId = order.RequestTransition(OrderTransitionTarget.Shipped);
        Assert.Throws<ConflictException>(() => order.RequestTransition(OrderTransitionTarget.Cancelled));
        order.CompleteTransition(transitionId);
        Assert.Throws<ConflictException>(() => order.RequestTransition(OrderTransitionTarget.Cancelled));
    }

    [Fact]
    public void CompleteRejectsUnknownTransition()
    {
        OrderAggregate order = CreateOrder();
        order.RequestTransition(OrderTransitionTarget.Shipped);
        Assert.Throws<ConflictException>(() => order.CompleteTransition(Guid.CreateVersion7()));
    }

    [Fact]
    public void HistoryRestoresTerminalStatus()
    {
        OrderAggregate source = CreateOrder();
        Guid transitionId = source.RequestTransition(OrderTransitionTarget.Cancelled);
        source.CompleteTransition(transitionId);
        var restored = new OrderAggregate();
        restored.LoadFromHistory(source.UncommittedEvents);

        Assert.Equal(OrderStatus.Cancelled, restored.Status);
        Assert.Empty(restored.UncommittedEvents);
    }

    [Fact]
    public void PlaceValidatesIdentityAndLineIdentity()
    {
        Assert.Throws<DomainException>(() => OrderAggregate.Place(
            Guid.Empty, "RDO-1", Guid.CreateVersion7(), ValidLines()));
        OrderLineDefinition[] lines = ValidLines();
        lines[0] = lines[0] with { Id = Guid.Empty };
        Assert.Throws<DomainException>(() => OrderAggregate.Place(
            Guid.CreateVersion7(), "RDO-1", Guid.CreateVersion7(), lines));
    }

    [Fact]
    public void UnknownTransitionAndHistoryEventsAreRejected()
    {
        OrderAggregate order = CreateOrder();
        Guid transition = order.RequestTransition((OrderTransitionTarget)999);
        Assert.Throws<InvalidOperationException>(() => order.CompleteTransition(transition));
        Assert.Throws<InvalidOperationException>(() => new OrderAggregate().LoadFromHistory([new OtherTestEvent()]));
    }

    private static OrderAggregate CreateOrder()
    {
        return OrderAggregate.Place(Guid.CreateVersion7(), "RDO-1", Guid.CreateVersion7(), ValidLines());
    }

    private static OrderLineDefinition[] ValidLines()
    {
        return
        [
            new OrderLineDefinition(Guid.CreateVersion7(), 1, Guid.CreateVersion7(), "EA", 2, 10, "USD", 2),
            new OrderLineDefinition(Guid.CreateVersion7(), 2, Guid.CreateVersion7(), "BOX", 1, 120, "USD", 12),
        ];
    }
}
