using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Order.Domain;

public sealed record OrderShipped(Guid TransitionId) : IDomainEvent;
