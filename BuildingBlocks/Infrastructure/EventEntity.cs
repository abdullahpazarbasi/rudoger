namespace Rudoger.BuildingBlocks.Infrastructure;

public sealed class EventEntity
{
    public Guid EventId { get; set; }

    public Guid StreamId { get; set; }

    public string AggregateType { get; set; } = string.Empty;

    public int Version { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int SchemaVersion { get; set; }

    public string Payload { get; set; } = string.Empty;

    public string Metadata { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }
}
