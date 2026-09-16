namespace Rudoger.Modules.Authn.Application;

public sealed record TokenResult(string AccessToken, string TokenType, int ExpiresIn);
