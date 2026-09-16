using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Product.Application;

namespace Rudoger.Modules.Product.Infrastructure;

public static class ProductInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddProductInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDbContext<ProductDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "product")));
        services.AddScoped<EventStore<ProductDbContext>>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryProductUsageGateway, InventoryProductUsageGateway>();
        services.AddScoped<IOrderProductUsageGateway, OrderProductUsageGateway>();
        return services;
    }
}
