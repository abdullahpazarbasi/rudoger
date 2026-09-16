using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Order.Presentation;
using Rudoger.Modules.Product.Application;

namespace Rudoger.Modules.Product.Infrastructure;

public sealed class OrderProductUsageGateway(IOrderInternalApi orderApi, IInternalCallLogger callLogger)
    : IOrderProductUsageGateway
{
    public Task<bool> HasAnyOrderAsync(Guid productId, CancellationToken cancellationToken)
    {
        return callLogger.ExecuteAsync(
            "Order.HasAnyOrder",
            token => orderApi.HasAnyOrderAsync(productId, token),
            cancellationToken);
    }
}
