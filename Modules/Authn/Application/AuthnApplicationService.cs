using Rudoger.Modules.Authn.Domain;

namespace Rudoger.Modules.Authn.Application;

public sealed class AuthnApplicationService(
    IAuthnRepository repository,
    IPasswordService passwordService,
    ITokenIssuer tokenIssuer)
{
    public async Task<TokenResult> ExchangeAsync(string username, string password, CancellationToken cancellationToken)
    {
        UserCredential? credential = await repository.GetByUsernameAsync(
            Normalize(username),
            cancellationToken);
        if (credential is null || !passwordService.Verify(credential.PasswordHash, password ?? string.Empty))
        {
            throw new UnauthorizedAccessException("The username or password is invalid.");
        }

        return tokenIssuer.Issue(credential.UserId, credential.Username);
    }

    public async Task EnsureUserAsync(string username, string password, CancellationToken cancellationToken)
    {
        string normalizedUsername = Normalize(username);
        if (await repository.GetByUsernameAsync(normalizedUsername, cancellationToken) is not null)
        {
            return;
        }

        UserAggregate user = UserAggregate.Register(
            Guid.CreateVersion7(),
            normalizedUsername,
            passwordService.Hash(password));
        await repository.SaveAsync(user, cancellationToken);
    }

    private static string Normalize(string? username)
    {
        return username?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
