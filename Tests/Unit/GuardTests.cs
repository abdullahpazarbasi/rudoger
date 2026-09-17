using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.UnitTests;

public sealed class GuardTests
{
    [Fact]
    public void RequiredTrimsValidValue()
    {
        Assert.Equal("value", Guard.Required(" value ", "code", "Field", 10));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequiredRejectsBlankValue(string? value)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Guard.Required(value, "required", "Field", 10));
        Assert.Equal("required", exception.Code);
    }

    [Fact]
    public void RequiredRejectsLongValue()
    {
        Assert.Throws<DomainException>(() => Guard.Required("elevenchars", "long", "Field", 10));
    }

    [Fact]
    public void PositiveAndNonNegativeApplyTheirBoundaries()
    {
        Assert.Equal(1m, Guard.Positive(1m, "positive", "Field"));
        Assert.Equal(0m, Guard.NonNegative(0m, "nonnegative", "Field"));
        Assert.Throws<DomainException>(() => Guard.Positive(0m, "positive", "Field"));
        Assert.Throws<DomainException>(() => Guard.NonNegative(-1m, "nonnegative", "Field"));
    }

    [Fact]
    public void DomainConflictAndConcurrencyExceptionsExposeTheirContracts()
    {
        var conflict = new ConflictException("conflict-code", "Conflict detail");
        var inner = new InvalidOperationException("inner");
        Guid aggregateId = Guid.CreateVersion7();
        var concurrency = new ConcurrencyException(aggregateId, inner);

        Assert.Equal("conflict-code", conflict.Code);
        Assert.Equal("Conflict detail", conflict.Message);
        Assert.Equal(aggregateId, concurrency.AggregateId);
        Assert.DoesNotContain("Stream", concurrency.Message, StringComparison.Ordinal);
        Assert.Same(inner, concurrency.InnerException);
    }
}
