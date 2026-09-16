using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.BuildingBlocks.Infrastructure;

public sealed record EventTypeRegistration(string EventType, Type ClrType)
{
    public static EventTypeRegistration Create<TEvent>(string eventType)
        where TEvent : IDomainEvent
    {
        return new EventTypeRegistration(eventType, typeof(TEvent));
    }
}
