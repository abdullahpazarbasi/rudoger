using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;

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
        ValidationException exception = Assert.Throws<ValidationException>(
            () => Paging.Normalize(pageNumber, pageSize));

        Assert.Equal("paging-invalid", exception.Code);
        Assert.DoesNotContain("Parameter", exception.Message, StringComparison.Ordinal);
    }
}
