using Rudoger.Modules.Authn.Application;

namespace Rudoger.Modules.Authn.Presentation;

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn)
{
    public static TokenResponse From(TokenResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TokenResponse(result.AccessToken, result.TokenType, result.ExpiresIn);
    }
}
