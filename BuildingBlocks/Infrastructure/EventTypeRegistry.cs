using System.Text.Json;
using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.BuildingBlocks.Infrastructure;

public sealed class EventTypeRegistry
{
    private readonly Dictionary<string, Type> _typesByName = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, string> _namesByType = [];
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public void Register(params EventTypeRegistration[] registrations)
    {
        foreach (EventTypeRegistration registration in registrations)
        {
            if (!_typesByName.TryAdd(registration.EventType, registration.ClrType)
                || !_namesByType.TryAdd(registration.ClrType, registration.EventType))
            {
                throw new InvalidOperationException($"Event type '{registration.EventType}' is already registered.");
            }
        }
    }

    public string NameOf(IDomainEvent domainEvent)
    {
        if (!_namesByType.TryGetValue(domainEvent.GetType(), out string? eventType))
        {
            throw new InvalidOperationException($"Event CLR type '{domainEvent.GetType().FullName}' is not registered.");
        }

        return eventType;
    }

    public string Serialize(IDomainEvent domainEvent)
    {
        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _serializerOptions);
    }

    public IDomainEvent Deserialize(string eventType, int schemaVersion, string payload)
    {
        if (schemaVersion != 1)
        {
            throw new InvalidOperationException($"Unsupported schema version '{schemaVersion}' for event '{eventType}'.");
        }

        if (!_typesByName.TryGetValue(eventType, out Type? clrType))
        {
            throw new InvalidOperationException($"Event type '{eventType}' is not registered.");
        }

        return (IDomainEvent)(JsonSerializer.Deserialize(payload, clrType, _serializerOptions)
            ?? throw new InvalidOperationException($"Event '{eventType}' could not be deserialized."));
    }
}
