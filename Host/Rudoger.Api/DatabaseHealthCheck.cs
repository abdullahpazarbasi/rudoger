using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rudoger.Modules.Authn.Infrastructure;

namespace Rudoger.Api;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        AuthnDbContext dbContext = scope.ServiceProvider.GetRequiredService<AuthnDbContext>();
        bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("The application database is reachable.")
            : HealthCheckResult.Unhealthy("The application database is not reachable.");
    }
}
