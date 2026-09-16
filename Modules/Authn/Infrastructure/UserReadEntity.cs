namespace Rudoger.Modules.Authn.Infrastructure;

public sealed class UserReadEntity
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
}
