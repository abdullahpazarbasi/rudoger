using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.BuildingBlocks.Application;

public static class Paging
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public static (int PageNumber, int PageSize) Normalize(int? pageNumber, int? pageSize)
    {
        int number = pageNumber ?? DefaultPageNumber;
        int size = pageSize ?? DefaultPageSize;

        if (number < 1)
        {
            throw new ValidationException("paging-invalid", "Page number must be at least one.");
        }

        if (size is < 1 or > MaximumPageSize)
        {
            throw new ValidationException(
                "paging-invalid",
                $"Page size must be between one and {MaximumPageSize}.");
        }

        return (number, size);
    }
}
