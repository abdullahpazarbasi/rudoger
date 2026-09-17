namespace Rudoger.BuildingBlocks.Domain;

public sealed class ValidationException : Exception
{
    public ValidationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
