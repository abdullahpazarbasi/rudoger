using Microsoft.AspNetCore.Identity;
using Rudoger.Modules.Authn.Application;

namespace Rudoger.Modules.Authn.Infrastructure;

public sealed class PasswordService(IPasswordHasher<string> passwordHasher) : IPasswordService
{
    // The hasher derives a per-hash random salt and ignores this argument, so the identity it wants
    // stays an implementation detail instead of leaking into the application contract.
    private const string HashSubject = "";

    public string Hash(string password)
    {
        return passwordHasher.HashPassword(HashSubject, password);
    }

    public bool Verify(string passwordHash, string suppliedPassword)
    {
        PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(
            HashSubject,
            passwordHash,
            suppliedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
