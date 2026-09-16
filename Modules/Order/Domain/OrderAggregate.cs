using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Order.Domain;

public sealed class OrderAggregate : AggregateRoot
{
    private readonly List<OrderLine> _lines = [];

    public string OrderNumber { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public Guid UserId { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public Guid? ActiveTransitionId { get; private set; }

    public OrderTransitionTarget? ActiveTransitionTarget { get; private set; }

    public IReadOnlyList<TransitionStockOperation> ActiveStockOperations { get; private set; } = [];

    public static OrderAggregate Place(Guid id, string orderNumber, Guid userId, IReadOnlyList<OrderLineDefinition> lines)
    {
        if (id == Guid.Empty || userId == Guid.Empty)
        {
            throw new DomainException("order-identity-invalid", "Order and user ids are required.");
        }

        string normalizedNumber = Guard.Required(
            orderNumber,
            "order-number-invalid",
            "Order number",
            OrderRules.OrderNumberMaximumLength);
        ValidateLines(lines);
        var aggregate = new OrderAggregate();
        aggregate.Raise(new OrderPlaced(id, normalizedNumber, userId, lines));
        return aggregate;
    }

    public Guid RequestTransition(OrderTransitionTarget target)
    {
        if (Status != OrderStatus.Placed)
        {
            throw new ConflictException("order-transition-invalid", $"An order in '{Status}' status cannot be transitioned.");
        }

        if (ActiveTransitionId.HasValue)
        {
            throw new ConflictException("order-transition-pending", "The order already has a pending transition.");
        }

        Guid transitionId = Guid.CreateVersion7();
        TransitionStockOperation[] stockOperations = _lines
            .Select(item => item.ProductId)
            .Distinct()
            .Order()
            .Select(productId => new TransitionStockOperation(productId, Guid.CreateVersion7()))
            .ToArray();
        Raise(new OrderTransitionRequested(transitionId, target, stockOperations));
        return transitionId;
    }

    public void CompleteTransition(Guid transitionId)
    {
        if (ActiveTransitionId != transitionId || !ActiveTransitionTarget.HasValue)
        {
            throw new ConflictException("order-transition-not-pending", "The requested order transition is not pending.");
        }

        Raise(ActiveTransitionTarget.Value switch
        {
            OrderTransitionTarget.Shipped => new OrderShipped(transitionId),
            OrderTransitionTarget.Cancelled => new OrderCancelled(transitionId),
            _ => throw new InvalidOperationException($"Unsupported transition target '{ActiveTransitionTarget.Value}'."),
        });
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case OrderPlaced placed:
                Id = placed.OrderId;
                OrderNumber = placed.OrderNumber;
                UserId = placed.UserId;
                Status = OrderStatus.Placed;
                _lines.AddRange(placed.Lines.Select(ToLine));
                break;
            case OrderTransitionRequested requested:
                ActiveTransitionId = requested.TransitionId;
                ActiveTransitionTarget = requested.Target;
                ActiveStockOperations = requested.StockOperations;
                break;
            case OrderShipped:
                Status = OrderStatus.Shipped;
                ClearTransition();
                break;
            case OrderCancelled:
                Status = OrderStatus.Cancelled;
                ClearTransition();
                break;
            default:
                throw new InvalidOperationException($"Unsupported order event '{domainEvent.GetType().Name}'.");
        }
    }

    private static void ValidateLines(IReadOnlyList<OrderLineDefinition> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count is < 1 or > OrderRules.MaximumLines)
        {
            throw new DomainException("order-lines-invalid", $"An order must contain between one and {OrderRules.MaximumLines} lines.");
        }

        if (lines.Select(item => item.Id).Distinct().Count() != lines.Count
            || lines.Select(item => item.Num).Distinct().Count() != lines.Count
            || lines.Select(item => (item.ProductId, item.UomCode)).Distinct().Count() != lines.Count)
        {
            throw new DomainException("order-lines-duplicate", "Order line ids, numbers, and Product/UoM pairs must be unique.");
        }

        if (lines.Select(item => item.UnitPriceCurrencyCode).Distinct(StringComparer.Ordinal).Count() != 1)
        {
            throw new DomainException("order-currency-mixed", "All order lines must use the same currency.");
        }

        foreach (OrderLineDefinition line in lines)
        {
            if (line.Id == Guid.Empty || line.ProductId == Guid.Empty || line.Num < 1)
            {
                throw new DomainException("order-line-identity-invalid", "Order line id, product id, and positive line number are required.");
            }

            Guard.Required(line.UomCode, "order-line-uom-invalid", "Order line UoM code", OrderRules.UomCodeMaximumLength);
            Guard.Positive(line.Quantity, "order-line-quantity-invalid", "Order line quantity");
            Guard.Positive(line.BaseQuantity, "order-line-base-quantity-invalid", "Order line base quantity");
            Guard.NonNegative(line.UnitPriceAmount, "order-line-price-invalid", "Order line unit price");
        }
    }

    private void ClearTransition()
    {
        ActiveTransitionId = null;
        ActiveTransitionTarget = null;
        ActiveStockOperations = [];
    }

    private static OrderLine ToLine(OrderLineDefinition definition)
    {
        return new OrderLine
        {
            Id = definition.Id,
            Num = definition.Num,
            ProductId = definition.ProductId,
            UomCode = definition.UomCode,
            Quantity = definition.Quantity,
            UnitPriceAmount = definition.UnitPriceAmount,
            UnitPriceCurrencyCode = definition.UnitPriceCurrencyCode,
            BaseQuantity = definition.BaseQuantity,
        };
    }
}
