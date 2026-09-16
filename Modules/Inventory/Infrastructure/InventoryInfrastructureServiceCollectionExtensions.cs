using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Inventory.Application;

namespace Rudoger.Modules.Inventory.Infrastructure;

public static class InventoryInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "inventory")));
        services.AddScoped<EventStore<InventoryDbContext>>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IProductInventoryGateway, ProductInventoryGateway>();
        services.AddHostedService<InventoryOutboxWorker>();
        return services;
    }
}
