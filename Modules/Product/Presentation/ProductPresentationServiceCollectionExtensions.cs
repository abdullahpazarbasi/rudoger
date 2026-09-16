using Microsoft.Extensions.DependencyInjection;
using Rudoger.Modules.Product.Application;

namespace Rudoger.Modules.Product.Presentation;

public static class ProductPresentationServiceCollectionExtensions
{
    public static IServiceCollection AddProductPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ProductApplicationService>();
        services.AddScoped<ProductUsageApplicationService>();
        services.AddScoped<IProductInternalApi, ProductInternalApi>();
        return services;
    }
}
