namespace Rudoger.BuildingBlocks.Domain;

public static class Guard
{
    public static string Required(string? value, string code, string fieldName, int maxLength)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new DomainException(code, $"{fieldName} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainException(code, $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    public static decimal NonNegative(decimal value, string code, string fieldName)
    {
        if (value < 0)
        {
            throw new DomainException(code, $"{fieldName} cannot be negative.");
        }

        return value;
    }

    public static decimal Positive(decimal value, string code, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainException(code, $"{fieldName} must be positive.");
        }

        return value;
    }
}
