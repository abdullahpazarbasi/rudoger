using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Presentation;

public sealed class OrderInternalApi(OrderApplicationService service) : IOrderInternalApi
{
    public Task<bool> HasAnyOrderAsync(Guid productId, CancellationToken cancellationToken)
    {
        return service.HasAnyOrderAsync(productId, cancellationToken);
    }
}
