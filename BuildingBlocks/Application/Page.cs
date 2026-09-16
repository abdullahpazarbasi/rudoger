namespace Rudoger.BuildingBlocks.Application;

public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount);
