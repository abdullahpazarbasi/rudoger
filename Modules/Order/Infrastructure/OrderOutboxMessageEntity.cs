namespace Rudoger.Modules.Order.Infrastructure;

public sealed class OrderOutboxMessageEntity
{
    public Guid Id { get; set; }

    public OrderWorkflowMessageType Type { get; set; }

    public Guid AggregateId { get; set; }

    public Guid? SecondaryId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public DateTimeOffset? LockedUntilUtc { get; set; }

    public DateTimeOffset NextAttemptAtUtc { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
