using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Authn.Domain;

public sealed record UserRegistered(Guid UserId, string Username, string PasswordHash) : IDomainEvent;
