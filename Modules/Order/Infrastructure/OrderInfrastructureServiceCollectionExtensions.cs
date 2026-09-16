using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Infrastructure;

public static class OrderInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddOrderInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDbContext<OrderDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "order")));
        services.AddScoped<EventStore<OrderDbContext>>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderPlacementRepository, OrderPlacementRepository>();
        services.AddScoped<IProductOrderGateway, ProductOrderGateway>();
        services.AddScoped<IInventoryOrderGateway, InventoryOrderGateway>();
        services.AddHostedService<OrderOutboxWorker>();
        return services;
    }
}
