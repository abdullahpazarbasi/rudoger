namespace Rudoger.BuildingBlocks.Domain;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
