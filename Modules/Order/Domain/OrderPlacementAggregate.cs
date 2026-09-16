using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Order.Domain;

public sealed class OrderPlacementAggregate : AggregateRoot
{
    private readonly List<OrderPlacementLine> _lines = [];

    public Guid OrderId { get; private set; }

    public Guid UserId { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public OrderPlacementStatus Status { get; private set; }

    public IReadOnlyList<OrderPlacementLine> Lines => _lines;

    public string? FailureCode { get; private set; }

    public string? FailureDetail { get; private set; }

    public static OrderPlacementAggregate Request(
        Guid placementId,
        Guid orderId,
        Guid userId,
        string idempotencyKey,
        IReadOnlyList<OrderPlacementLine> lines)
    {
        if (placementId == Guid.Empty || orderId == Guid.Empty || userId == Guid.Empty)
        {
            throw new DomainException("order-placement-identity-invalid", "Placement, order, and user ids are required.");
        }

        string key = Guard.Required(
            idempotencyKey,
            "order-idempotency-key-invalid",
            "Idempotency key",
            OrderRules.IdempotencyKeyMaximumLength);
        ValidateLines(lines);
        var aggregate = new OrderPlacementAggregate();
        aggregate.Raise(new OrderPlacementRequested(placementId, orderId, userId, key, lines));
        return aggregate;
    }

    public void Succeed()
    {
        EnsurePending();
        Raise(new OrderPlacementSucceeded());
    }

    public void Fail(string code, string detail)
    {
        EnsurePending();
        string failureCode = Guard.Required(code, "order-placement-failure-code-invalid", "Failure code", OrderRules.FailureCodeMaximumLength);
        string failureDetail = Guard.Required(
            detail,
            "order-placement-failure-detail-invalid",
            "Failure detail",
            OrderRules.FailureDetailMaximumLength);
        Raise(new OrderPlacementFailed(failureCode, failureDetail));
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case OrderPlacementRequested requested:
                Id = requested.PlacementId;
                OrderId = requested.OrderId;
                UserId = requested.UserId;
                IdempotencyKey = requested.IdempotencyKey;
                Status = OrderPlacementStatus.Pending;
                _lines.AddRange(requested.Lines);
                break;
            case OrderPlacementSucceeded:
                Status = OrderPlacementStatus.Succeeded;
                break;
            case OrderPlacementFailed failed:
                Status = OrderPlacementStatus.Failed;
                FailureCode = failed.FailureCode;
                FailureDetail = failed.FailureDetail;
                break;
            default:
                throw new InvalidOperationException($"Unsupported order placement event '{domainEvent.GetType().Name}'.");
        }
    }

    private static void ValidateLines(IReadOnlyList<OrderPlacementLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count is < 1 or > OrderRules.MaximumLines)
        {
            throw new DomainException("order-lines-invalid", $"An order placement must contain between one and {OrderRules.MaximumLines} lines.");
        }

        if (lines.Select(item => item.Num).Distinct().Count() != lines.Count
            || lines.Select(item => (item.ProductId, item.UomCode.Trim().ToUpperInvariant())).Distinct().Count() != lines.Count)
        {
            throw new DomainException("order-lines-duplicate", "Line numbers and Product/UoM pairs must be unique.");
        }

        foreach (OrderPlacementLine line in lines)
        {
            if (line.Id == Guid.Empty
                || line.ProductId == Guid.Empty
                || line.ReservationSourceEventId == Guid.Empty
                || line.ReleaseSourceEventId == Guid.Empty
                || line.Num < 1)
            {
                throw new DomainException("order-line-identity-invalid", "Order placement line identities are invalid.");
            }

            Guard.Required(line.UomCode, "order-line-uom-invalid", "Order line UoM code", OrderRules.UomCodeMaximumLength);
            Guard.Positive(line.Quantity, "order-line-quantity-invalid", "Order line quantity");
        }
    }

    private void EnsurePending()
    {
        if (Status != OrderPlacementStatus.Pending)
        {
            throw new ConflictException("order-placement-completed", "The order placement is already complete.");
        }
    }
}
