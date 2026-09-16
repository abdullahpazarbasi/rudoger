using Microsoft.Extensions.DependencyInjection;

namespace Rudoger.Modules.Logging.Presentation;

public static class LoggingPresentationServiceCollectionExtensions
{
    public static IServiceCollection AddLoggingPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<SensitiveDataMasker>();
        return services;
    }
}
