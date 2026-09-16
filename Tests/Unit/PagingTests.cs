using Rudoger.BuildingBlocks.Application;

namespace Rudoger.UnitTests;

public sealed class PagingTests
{
    [Fact]
    public void NormalizeUsesDefaults()
    {
        Assert.Equal((1, 20), Paging.Normalize(null, null));
    }

    [Fact]
    public void NormalizePreservesValidValues()
    {
        Assert.Equal((3, 100), Paging.Normalize(3, 100));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void NormalizeRejectsInvalidValues(int pageNumber, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Paging.Normalize(pageNumber, pageSize));
    }
}
