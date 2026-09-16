using Microsoft.AspNetCore.Identity;
using Rudoger.Modules.Authn.Application;

namespace Rudoger.Modules.Authn.Infrastructure;

public sealed class PasswordService(IPasswordHasher<string> passwordHasher) : IPasswordService
{
    public string Hash(string username, string password)
    {
        return passwordHasher.HashPassword(username, password);
    }

    public bool Verify(string username, string passwordHash, string suppliedPassword)
    {
        PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(username, passwordHash, suppliedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
