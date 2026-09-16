using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Product.Domain;

public sealed record ProductPackagingAdded(PackagingDefinition Packaging) : IDomainEvent;
