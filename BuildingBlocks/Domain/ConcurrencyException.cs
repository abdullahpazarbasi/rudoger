namespace Rudoger.BuildingBlocks.Domain;

public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException(Guid streamId, Exception? innerException = null)
        : base($"Stream '{streamId}' was modified concurrently.", innerException)
    {
        StreamId = streamId;
    }

    public Guid StreamId { get; }
}
