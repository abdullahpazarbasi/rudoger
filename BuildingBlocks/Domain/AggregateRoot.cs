namespace Rudoger.BuildingBlocks.Domain;

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];

    public Guid Id { get; protected set; }

    public int Version { get; private set; }

    public int OriginalVersion => Version - _uncommittedEvents.Count;

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    public void LoadFromHistory(IEnumerable<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (IDomainEvent domainEvent in events)
        {
            Apply(domainEvent);
            Version++;
        }
    }

    public void MarkChangesAsCommitted()
    {
        _uncommittedEvents.Clear();
    }

    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        Apply(domainEvent);
        _uncommittedEvents.Add(domainEvent);
        Version++;
    }

    protected abstract void Apply(IDomainEvent domainEvent);
}
