using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Authn.Application;

namespace Rudoger.Modules.Authn.Infrastructure;

public static class AuthnInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAuthnInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddDbContext<AuthnDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "authn")));
        services.AddScoped<EventStore<AuthnDbContext>>();
        services.AddScoped<IAuthnRepository, AuthnRepository>();
        services.AddSingleton<IPasswordHasher<string>, PasswordHasher<string>>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();

        JwtOptions jwt = configuration.GetRequiredSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");
        ValidateJwt(jwt);
        services.Configure<JwtOptions>(configuration.GetRequiredSection(JwtOptions.SectionName));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "preferred_username",
            };
        });
        services.AddAuthorization();
        return services;
    }

    private static void ValidateJwt(JwtOptions jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt.Issuer)
            || string.IsNullOrWhiteSpace(jwt.Audience)
            || jwt.Secret.Length < 32
            || jwt.LifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("JWT issuer, audience, a secret of at least 32 characters, and a positive lifetime are required.");
        }
    }
}
