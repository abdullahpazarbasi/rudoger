using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.Modules.Authn.Domain;
using Rudoger.Modules.Inventory.Domain;
using Rudoger.Modules.Order.Domain;
using Rudoger.Modules.Product.Domain;

namespace Rudoger.Api;

public static class RudogerEventTypeRegistry
{
    public static EventTypeRegistry Create()
    {
        var registry = new EventTypeRegistry();
        registry.Register(
            EventTypeRegistration.Create<UserRegistered>("authn.user-registered"),
            EventTypeRegistration.Create<ProductCreated>("product.product-created"),
            EventTypeRegistration.Create<ProductChanged>("product.product-changed"),
            EventTypeRegistration.Create<ProductDeleted>("product.product-deleted"),
            EventTypeRegistration.Create<ProductPackagingAdded>("product.packaging-added"),
            EventTypeRegistration.Create<ProductPackagingChanged>("product.packaging-changed"),
            EventTypeRegistration.Create<ProductPackagingRemoved>("product.packaging-removed"),
            EventTypeRegistration.Create<ProductUsageClaimed>("product.usage-claimed"),
            EventTypeRegistration.Create<ProductUsageReleased>("product.usage-released"),
            EventTypeRegistration.Create<StockItemOpened>("inventory.stock-item-opened"),
            EventTypeRegistration.Create<StockReceived>("inventory.stock-received"),
            EventTypeRegistration.Create<StockAdjusted>("inventory.stock-adjusted"),
            EventTypeRegistration.Create<StockDeducted>("inventory.stock-deducted"),
            EventTypeRegistration.Create<StockReserved>("inventory.stock-reserved"),
            EventTypeRegistration.Create<StockCommitted>("inventory.stock-committed"),
            EventTypeRegistration.Create<StockReleased>("inventory.stock-released"),
            EventTypeRegistration.Create<OrderPlaced>("order.order-placed"),
            EventTypeRegistration.Create<OrderTransitionRequested>("order.transition-requested"),
            EventTypeRegistration.Create<OrderShipped>("order.order-shipped"),
            EventTypeRegistration.Create<OrderCancelled>("order.order-cancelled"),
            EventTypeRegistration.Create<OrderPlacementRequested>("order.placement-requested"),
            EventTypeRegistration.Create<OrderPlacementSucceeded>("order.placement-succeeded"),
            EventTypeRegistration.Create<OrderPlacementFailed>("order.placement-failed"));
        return registry;
    }
}
