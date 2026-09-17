using Rudoger.BuildingBlocks.Application;

namespace Rudoger.BuildingBlocks.Presentation;

public static class PageResponseFactory
{
    public static PageResponse<TResponse> From<TItem, TResponse>(Page<TItem> page, Func<TItem, TResponse> map)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(map);
        return new PageResponse<TResponse>(
            page.Items.Select(map).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount);
    }
}
