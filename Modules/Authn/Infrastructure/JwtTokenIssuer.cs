using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rudoger.Modules.Authn.Application;

namespace Rudoger.Modules.Authn.Infrastructure;

public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenIssuer
{
    public TokenResult Issue(Guid userId, string username)
    {
        JwtOptions jwt = options.Value;
        DateTimeOffset now = timeProvider.GetUtcNow();
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            jwt.Issuer,
            jwt.Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("preferred_username", username),
            ],
            now.UtcDateTime,
            now.AddMinutes(jwt.LifetimeMinutes).UtcDateTime,
            signingCredentials);
        string serialized = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResult(serialized, "Bearer", checked(jwt.LifetimeMinutes * 60));
    }
}
