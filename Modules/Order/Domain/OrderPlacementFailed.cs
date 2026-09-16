using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Order.Domain;

public sealed record OrderPlacementFailed(string FailureCode, string FailureDetail) : IDomainEvent;
