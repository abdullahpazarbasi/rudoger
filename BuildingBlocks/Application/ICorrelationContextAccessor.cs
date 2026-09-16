namespace Rudoger.BuildingBlocks.Application;

public interface ICorrelationContextAccessor
{
    CorrelationContext Current { get; set; }
}
