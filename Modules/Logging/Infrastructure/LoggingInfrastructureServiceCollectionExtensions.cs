using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Logging.Application;

namespace Rudoger.Modules.Logging.Infrastructure;

public static class LoggingInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRequestLoggingInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDbContext<LoggingDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "logging")));
        services.AddScoped<IRequestLogStore, RequestLogStore>();
        services.AddScoped<IInternalCallLogger, InternalCallLogger>();
        return services;
    }
}
