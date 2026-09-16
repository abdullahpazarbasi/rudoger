using Rudoger.Modules.Authn.Domain;

namespace Rudoger.Modules.Authn.Application;

public sealed class AuthnApplicationService(
    IAuthnRepository repository,
    IPasswordService passwordService,
    ITokenIssuer tokenIssuer)
{
    public async Task<TokenResult> ExchangeAsync(string username, string password, CancellationToken cancellationToken)
    {
        string normalizedUsername = username?.Trim().ToLowerInvariant() ?? string.Empty;
        UserCredential? credential = await repository.GetByUsernameAsync(normalizedUsername, cancellationToken);
        if (credential is null || !passwordService.Verify(credential.Username, credential.PasswordHash, password ?? string.Empty))
        {
            throw new UnauthorizedAccessException("The username or password is invalid.");
        }

        return tokenIssuer.Issue(credential.UserId, credential.Username);
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (string username in new[] { "abdullah", "murat", "gokhan" })
        {
            if (await repository.GetByUsernameAsync(username, cancellationToken) is not null)
            {
                continue;
            }

            UserAggregate user = UserAggregate.Register(
                Guid.CreateVersion7(),
                username,
                passwordService.Hash(username, "12345678"));
            await repository.SaveAsync(user, cancellationToken);
        }
    }
}
