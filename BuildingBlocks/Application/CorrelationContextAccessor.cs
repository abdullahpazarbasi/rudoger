namespace Rudoger.BuildingBlocks.Application;

public sealed class CorrelationContextAccessor : ICorrelationContextAccessor
{
    private static readonly AsyncLocal<CorrelationContext?> AmbientContext = new();

    public CorrelationContext Current
    {
        get => AmbientContext.Value ?? new CorrelationContext(Guid.CreateVersion7().ToString("N"));
        set => AmbientContext.Value = value;
    }
}
