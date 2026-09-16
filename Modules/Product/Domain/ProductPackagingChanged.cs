using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Product.Domain;

public sealed record ProductPackagingChanged(PackagingDefinition Packaging) : IDomainEvent;
