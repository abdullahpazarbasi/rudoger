namespace Rudoger.BuildingBlocks.Application;

public sealed record CorrelationContext(string CorrelationId, Guid? UserId = null, Guid? CausationId = null);
