using Microsoft.EntityFrameworkCore;
using Rudoger.Modules.Authn.Application;
using Rudoger.Modules.Authn.Infrastructure;
using Rudoger.Modules.Inventory.Infrastructure;
using Rudoger.Modules.Logging.Infrastructure;
using Rudoger.Modules.Order.Infrastructure;
using Rudoger.Modules.Product.Infrastructure;

namespace Rudoger.Api;

public static class DatabaseLifecycle
{
    public static async Task MigrateAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AuthnDbContext>().Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<ProductDbContext>().Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<OrderDbContext>().Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<LoggingDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AuthnApplicationService>().SeedAsync(cancellationToken);
    }
}
