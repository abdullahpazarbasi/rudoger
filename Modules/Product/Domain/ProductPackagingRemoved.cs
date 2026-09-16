using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Product.Domain;

public sealed record ProductPackagingRemoved(Guid PackagingId) : IDomainEvent;
