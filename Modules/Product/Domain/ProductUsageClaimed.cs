using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Product.Domain;

public sealed record ProductUsageClaimed(Guid OperationId, string UsageType) : IDomainEvent;
