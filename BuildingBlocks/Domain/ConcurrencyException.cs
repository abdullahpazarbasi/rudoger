namespace Rudoger.BuildingBlocks.Domain;

public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException(Guid aggregateId, Exception? innerException = null)
        : base("The resource was modified concurrently. Retry the operation.", innerException)
    {
        AggregateId = aggregateId;
    }

    public Guid AggregateId { get; }
}
