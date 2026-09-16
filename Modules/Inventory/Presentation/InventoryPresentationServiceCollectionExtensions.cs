using Microsoft.Extensions.DependencyInjection;
using Rudoger.Modules.Inventory.Application;

namespace Rudoger.Modules.Inventory.Presentation;

public static class InventoryPresentationServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<InventoryApplicationService>();
        services.AddScoped<InventoryInternalService>();
        services.AddScoped<IInventoryInternalApi, InventoryInternalApi>();
        return services;
    }
}
