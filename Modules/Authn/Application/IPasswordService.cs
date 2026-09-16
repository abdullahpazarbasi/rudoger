namespace Rudoger.Modules.Authn.Application;

public interface IPasswordService
{
    string Hash(string username, string password);

    bool Verify(string username, string passwordHash, string suppliedPassword);
}
