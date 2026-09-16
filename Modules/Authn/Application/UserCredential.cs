namespace Rudoger.Modules.Authn.Application;

public sealed record UserCredential(Guid UserId, string Username, string PasswordHash);
