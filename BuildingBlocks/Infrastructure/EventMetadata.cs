namespace Rudoger.BuildingBlocks.Infrastructure;

public sealed record EventMetadata(string CorrelationId, Guid? CausationId, Guid? ActorId);
