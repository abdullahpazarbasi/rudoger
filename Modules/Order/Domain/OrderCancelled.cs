using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Order.Domain;

public sealed record OrderCancelled(Guid TransitionId) : IDomainEvent;
