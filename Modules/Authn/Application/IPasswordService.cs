namespace Rudoger.Modules.Authn.Application;

public interface IPasswordService
{
    string Hash(string password);

    bool Verify(string passwordHash, string suppliedPassword);
}
