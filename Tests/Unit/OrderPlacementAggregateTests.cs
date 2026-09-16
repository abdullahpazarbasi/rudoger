using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Order.Domain;

namespace Rudoger.UnitTests;

public sealed class OrderPlacementAggregateTests
{
    [Fact]
    public void RequestCreatesPendingPlacement()
    {
        OrderPlacementAggregate placement = CreatePlacement();
        Assert.Equal(OrderPlacementStatus.Pending, placement.Status);
        Assert.Equal(2, placement.Lines.Count);
        Assert.IsType<OrderPlacementRequested>(Assert.Single(placement.UncommittedEvents));
    }

    [Fact]
    public void RequestRejectsDuplicateProductUom()
    {
        OrderPlacementLine[] lines = ValidLines();
        lines[1] = lines[1] with { ProductId = lines[0].ProductId, UomCode = lines[0].UomCode };
        Assert.Throws<DomainException>(() => OrderPlacementAggregate.Request(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "key", lines));
    }

    [Fact]
    public void SucceedIsTerminal()
    {
        OrderPlacementAggregate placement = CreatePlacement();
        placement.Succeed();
        Assert.Equal(OrderPlacementStatus.Succeeded, placement.Status);
        Assert.Throws<ConflictException>(placement.Succeed);
        Assert.Throws<ConflictException>(() => placement.Fail("failure", "detail"));
    }

    [Fact]
    public void FailCapturesReasonAndIsTerminal()
    {
        OrderPlacementAggregate placement = CreatePlacement();
        placement.Fail("insufficient-stock", "Not enough stock.");
        Assert.Equal(OrderPlacementStatus.Failed, placement.Status);
        Assert.Equal("insufficient-stock", placement.FailureCode);
        Assert.Equal("Not enough stock.", placement.FailureDetail);
        Assert.Throws<ConflictException>(placement.Succeed);
    }

    [Fact]
    public void FailureFieldsAreValidated()
    {
        OrderPlacementAggregate placement = CreatePlacement();
        Assert.Throws<DomainException>(() => placement.Fail(string.Empty, "detail"));
        Assert.Throws<DomainException>(() => placement.Fail("code", string.Empty));
    }

    [Fact]
    public void RequestValidatesIdentitiesLineCountAndLineIdentity()
    {
        Assert.Throws<DomainException>(() => OrderPlacementAggregate.Request(
            Guid.Empty, Guid.CreateVersion7(), Guid.CreateVersion7(), "key", ValidLines()));
        Assert.Throws<DomainException>(() => OrderPlacementAggregate.Request(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "key", []));
        OrderPlacementLine[] lines = ValidLines();
        lines[0] = lines[0] with { Id = Guid.Empty };
        Assert.Throws<DomainException>(() => OrderPlacementAggregate.Request(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "key", lines));
    }

    [Fact]
    public void HistoryRejectsUnknownEvent()
    {
        var placement = new OrderPlacementAggregate();
        Assert.Throws<InvalidOperationException>(() => placement.LoadFromHistory([new OtherTestEvent()]));
    }

    private static OrderPlacementAggregate CreatePlacement()
    {
        return OrderPlacementAggregate.Request(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "key",
            ValidLines());
    }

    private static OrderPlacementLine[] ValidLines()
    {
        return
        [
            new OrderPlacementLine(Guid.CreateVersion7(), 1, Guid.CreateVersion7(), "EA", 2, Guid.CreateVersion7(), Guid.CreateVersion7()),
            new OrderPlacementLine(Guid.CreateVersion7(), 2, Guid.CreateVersion7(), "BOX", 1, Guid.CreateVersion7(), Guid.CreateVersion7()),
        ];
    }
}
