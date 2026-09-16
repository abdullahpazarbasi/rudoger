using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Product.Domain;

public sealed record ProductUsageReleased(Guid OperationId) : IDomainEvent;
