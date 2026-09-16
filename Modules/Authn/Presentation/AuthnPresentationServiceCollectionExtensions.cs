using Microsoft.Extensions.DependencyInjection;
using Rudoger.Modules.Authn.Application;

namespace Rudoger.Modules.Authn.Presentation;

public static class AuthnPresentationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthnPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<AuthnApplicationService>();
        return services;
    }
}
