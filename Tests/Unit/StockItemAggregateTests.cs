using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Inventory.Domain;

namespace Rudoger.UnitTests;

public sealed class StockItemAggregateTests
{
    [Fact]
    public void OpenCreatesReceiptForPositiveOpeningQuantity()
    {
        StockItemAggregate stock = Open(10);

        Assert.Equal(10, stock.OnHandQuantity);
        Assert.Equal(10, stock.AvailableQuantity);
        Assert.Collection(
            stock.UncommittedEvents,
            item => Assert.IsType<StockItemOpened>(item),
            item => Assert.IsType<StockReceived>(item));
    }

    [Fact]
    public void OpenWithZeroDoesNotCreateMovement()
    {
        StockItemAggregate stock = Open(0);
        Assert.IsType<StockItemOpened>(Assert.Single(stock.UncommittedEvents));
    }

    [Fact]
    public void ReceiveAdjustAndDeductUpdateBalances()
    {
        StockItemAggregate stock = Open(10);
        stock.Receive(5, "receipt", "correlation", Guid.CreateVersion7(), "EA", 5);
        stock.Adjust(-2, "adjust", "correlation", Guid.CreateVersion7(), "EA", -2);
        stock.Deduct(3, "deduct", "correlation", Guid.CreateVersion7(), "EA", 3);

        Assert.Equal(10, stock.OnHandQuantity);
        Assert.Equal(10, stock.AvailableQuantity);
    }

    [Fact]
    public void ManualMovementPreservesRequestedUomAndQuantity()
    {
        StockItemAggregate stock = Open(10);

        stock.Receive(20, "receipt", "correlation", Guid.CreateVersion7(), "case", 2);

        StockReceived received = Assert.IsType<StockReceived>(stock.UncommittedEvents[^1]);
        Assert.Equal("CASE", received.Movement.UomCode);
        Assert.Equal(2, received.Movement.Quantity);
        Assert.Equal(20, received.Movement.OnHandQuantityDelta);
    }

    [Fact]
    public void AdjustmentAndDeductionCannotViolateAvailableStock()
    {
        StockItemAggregate stock = Open(10);
        stock.Reserve(8, Guid.CreateVersion7(), "correlation", Guid.CreateVersion7());

        Assert.Throws<DomainException>(() => stock.Adjust(-3, "adjust", "correlation", Guid.CreateVersion7(), "EA", -3));
        Assert.Throws<ConflictException>(() => stock.Deduct(3, "deduct", "correlation", Guid.CreateVersion7(), "EA", 3));
    }

    [Fact]
    public void ReserveAndCommitUpdateBothBalances()
    {
        StockItemAggregate stock = Open(10);
        Guid orderId = Guid.CreateVersion7();
        stock.Reserve(4, orderId, "correlation", Guid.CreateVersion7());
        Assert.Equal(4, stock.ReservedQuantity);
        Assert.Equal(6, stock.AvailableQuantity);

        stock.Commit(orderId, "correlation", Guid.CreateVersion7());
        Assert.Equal(6, stock.OnHandQuantity);
        Assert.Equal(0, stock.ReservedQuantity);
    }

    [Fact]
    public void ReserveAndReleaseRestoreAvailability()
    {
        StockItemAggregate stock = Open(10);
        Guid orderId = Guid.CreateVersion7();
        stock.Reserve(4, orderId, "correlation", Guid.CreateVersion7());
        stock.Release(orderId, "correlation", Guid.CreateVersion7());

        Assert.Equal(10, stock.OnHandQuantity);
        Assert.Equal(0, stock.ReservedQuantity);
        Assert.Equal(10, stock.AvailableQuantity);
    }

    [Fact]
    public void ReservationOperationIsIdempotentWhileActive()
    {
        StockItemAggregate stock = Open(10);
        Guid orderId = Guid.CreateVersion7();
        Guid operationId = Guid.CreateVersion7();
        stock.Reserve(4, orderId, "correlation", operationId);
        int version = stock.Version;

        stock.Reserve(4, orderId, "correlation", operationId);
        Assert.Equal(version, stock.Version);
    }

    [Fact]
    public void CompensatedReservationCannotBeReplayedAsActive()
    {
        StockItemAggregate stock = Open(10);
        Guid orderId = Guid.CreateVersion7();
        Guid reserveId = Guid.CreateVersion7();
        stock.Reserve(4, orderId, "correlation", reserveId);
        stock.ReleaseIfPresent(orderId, "correlation", Guid.CreateVersion7());

        Assert.Throws<ConflictException>(() => stock.Reserve(4, orderId, "correlation", reserveId));
    }

    [Fact]
    public void CompensatingReleaseIsSafeWhenReservationIsAbsent()
    {
        StockItemAggregate stock = Open(10);
        int version = stock.Version;
        stock.ReleaseIfPresent(Guid.CreateVersion7(), "correlation", Guid.CreateVersion7());
        Assert.Equal(version, stock.Version);
    }

    [Fact]
    public void MovementInputsAreValidated()
    {
        StockItemAggregate stock = Open(10);
        Assert.Throws<DomainException>(() => stock.Receive(0, "key", "correlation", Guid.CreateVersion7(), "EA", 0));
        Assert.Throws<DomainException>(() => stock.Adjust(0, "key", "correlation", Guid.CreateVersion7(), "EA", 0));
        Assert.Throws<DomainException>(() => stock.Adjust(-1, "key", "correlation", Guid.CreateVersion7(), "EA", 1));
        Assert.Throws<DomainException>(() => stock.Reserve(1, Guid.Empty, "correlation", Guid.CreateVersion7()));
        Assert.Throws<DomainException>(() => stock.Receive(1, string.Empty, "correlation", Guid.CreateVersion7(), "EA", 1));
        Assert.Throws<DomainException>(() => stock.Receive(1, "key", "correlation", Guid.CreateVersion7(), string.Empty, 1));
    }

    [Fact]
    public void HistoryRestoresReservationsAndProcessedEvents()
    {
        StockItemAggregate source = Open(10);
        Guid orderId = Guid.CreateVersion7();
        Guid sourceId = Guid.CreateVersion7();
        source.Reserve(2, orderId, "correlation", sourceId);
        var restored = new StockItemAggregate();
        restored.LoadFromHistory(source.UncommittedEvents);

        Assert.Equal(2, restored.ReservedQuantity);
        restored.Reserve(2, orderId, "correlation", sourceId);
        Assert.Empty(restored.UncommittedEvents);
    }

    [Fact]
    public void OpenValidatesIdentityQuantityAndIdempotencyKey()
    {
        Assert.Throws<DomainException>(() => StockItemAggregate.Open(
            Guid.Empty, Guid.CreateVersion7(), "EA", 0, "key", "correlation", Guid.CreateVersion7(), "EA", 0));
        Assert.Throws<DomainException>(() => StockItemAggregate.Open(
            Guid.CreateVersion7(), Guid.Empty, "EA", 0, "key", "correlation", Guid.CreateVersion7(), "EA", 0));
        Assert.Throws<DomainException>(() => StockItemAggregate.Open(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "EA", -1, "key", "correlation", Guid.CreateVersion7(), "EA", -1));
        Assert.Throws<DomainException>(() => StockItemAggregate.Open(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "EA", 0, string.Empty, "correlation", Guid.CreateVersion7(), "EA", 0));
        Assert.Throws<DomainException>(() => StockItemAggregate.Open(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "EA", 0, "key", "correlation", Guid.CreateVersion7(), "EA", 1));
    }

    [Fact]
    public void ManualMovementsAreIdempotentByOperation()
    {
        StockItemAggregate stock = Open(10);
        Guid receipt = Guid.CreateVersion7();
        Guid adjustment = Guid.CreateVersion7();
        Guid deduction = Guid.CreateVersion7();
        stock.Receive(1, "receipt", "correlation", receipt, "EA", 1);
        stock.Receive(1, "receipt", "correlation", receipt, "EA", 1);
        stock.Adjust(1, "adjust", "correlation", adjustment, "EA", 1);
        stock.Adjust(1, "adjust", "correlation", adjustment, "EA", 1);
        stock.Deduct(1, "deduct", "correlation", deduction, "EA", 1);
        int version = stock.Version;
        stock.Deduct(1, "deduct", "correlation", deduction, "EA", 1);

        Assert.Equal(version, stock.Version);
        Assert.Equal(11, stock.OnHandQuantity);
    }

    [Fact]
    public void ReservationCommandsValidateConflictsAndAreIdempotent()
    {
        StockItemAggregate stock = Open(10);
        Guid orderId = Guid.CreateVersion7();
        stock.Reserve(2, orderId, "correlation", Guid.CreateVersion7());
        Assert.Throws<ConflictException>(() => stock.Reserve(2, orderId, "correlation", Guid.CreateVersion7()));
        Assert.Throws<ConflictException>(() => stock.Reserve(20, Guid.CreateVersion7(), "correlation", Guid.CreateVersion7()));
        Assert.Throws<ConflictException>(() => stock.Commit(Guid.CreateVersion7(), "correlation", Guid.CreateVersion7()));
        Assert.Throws<ConflictException>(() => stock.Release(Guid.CreateVersion7(), "correlation", Guid.CreateVersion7()));

        Guid commitId = Guid.CreateVersion7();
        stock.Commit(orderId, "correlation", commitId);
        int version = stock.Version;
        stock.Commit(orderId, "correlation", commitId);
        Assert.Equal(version, stock.Version);
    }

    [Fact]
    public void MovementMetadataAndHistoryAreValidated()
    {
        StockItemAggregate stock = Open(10);
        Assert.Throws<DomainException>(() => stock.Receive(1, "key", "correlation", Guid.Empty, "EA", 1));
        Assert.Throws<DomainException>(() => stock.Receive(1, "key", string.Empty, Guid.CreateVersion7(), "EA", 1));
        Assert.Throws<InvalidOperationException>(() => new StockItemAggregate().LoadFromHistory([new OtherTestEvent()]));

        Guid movementId = Guid.CreateVersion7();
        Guid referenceId = Guid.CreateVersion7();
        Guid operationId = Guid.CreateVersion7();
        var data = new StockMovementData(
            movementId,
            1,
            2,
            "ORDER",
            referenceId,
            "key",
            "correlation",
            operationId,
            "EA",
            1);
        Assert.Equal(movementId, data.MovementId);
        Assert.Equal(1, data.OnHandQuantityDelta);
        Assert.Equal(2, data.ReservedQuantityDelta);
        Assert.Equal("ORDER", data.ReferenceType);
        Assert.Equal(referenceId, data.ReferenceId);
        Assert.Equal("key", data.IdempotencyKey);
        Assert.Equal("correlation", data.CorrelationId);
        Assert.Equal(operationId, data.OperationId);
        Assert.Equal("EA", data.UomCode);
        Assert.Equal(1, data.Quantity);
    }

    private static StockItemAggregate Open(decimal quantity)
    {
        return StockItemAggregate.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "ea",
            quantity,
            "opening",
            "correlation",
            Guid.CreateVersion7(),
            "EA",
            quantity);
    }
}
