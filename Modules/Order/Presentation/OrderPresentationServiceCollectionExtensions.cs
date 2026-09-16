using Microsoft.Extensions.DependencyInjection;
using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Presentation;

public static class OrderPresentationServiceCollectionExtensions
{
    public static IServiceCollection AddOrderPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<OrderApplicationService>();
        services.AddScoped<OrderWorkflowService>();
        services.AddScoped<IOrderInternalApi, OrderInternalApi>();
        return services;
    }
}
