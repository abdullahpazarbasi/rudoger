using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.UnitTests;

public sealed record TestEvent(string Value) : IDomainEvent;
