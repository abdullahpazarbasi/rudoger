namespace Rudoger.BuildingBlocks.Presentation;

public sealed record PageResponse<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount);
