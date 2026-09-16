namespace Rudoger.Modules.Authn.Application;

public interface ITokenIssuer
{
    TokenResult Issue(Guid userId, string username);
}
