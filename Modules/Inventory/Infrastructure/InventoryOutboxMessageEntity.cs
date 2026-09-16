namespace Rudoger.Modules.Inventory.Infrastructure;

public sealed class InventoryOutboxMessageEntity
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid OperationId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset NextAttemptAtUtc { get; set; }

    public DateTimeOffset? LockedUntilUtc { get; set; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
