using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.Modules.Inventory.Domain;

public sealed class StockItemAggregate : AggregateRoot
{
    private readonly Dictionary<Guid, decimal> _reservations = [];
    private readonly HashSet<Guid> _processedSourceEvents = [];

    public Guid ProductId { get; private set; }

    public string BaseUomCode { get; private set; } = string.Empty;

    public decimal OnHandQuantity { get; private set; }

    public decimal ReservedQuantity { get; private set; }

    public decimal AvailableQuantity => OnHandQuantity - ReservedQuantity;

    public static StockItemAggregate Open(
        Guid stockItemId,
        Guid productId,
        string baseUomCode,
        decimal openingQuantity,
        string idempotencyKey,
        string correlationId,
        Guid sourceEventId)
    {
        if (stockItemId == Guid.Empty || productId == Guid.Empty)
        {
            throw new DomainException("stock-item-id-invalid", "Stock item and product ids are required.");
        }

        string normalizedUom = Guard.Required(
            baseUomCode,
            "stock-item-uom-invalid",
            "Base UoM code",
            InventoryRules.UomCodeMaximumLength).ToUpperInvariant();
        Guard.NonNegative(openingQuantity, "stock-opening-quantity-invalid", "Opening quantity");
        string key = Guard.Required(
            idempotencyKey,
            "stock-idempotency-key-invalid",
            "Idempotency key",
            InventoryRules.IdempotencyKeyMaximumLength);

        var aggregate = new StockItemAggregate();
        aggregate.Raise(new StockItemOpened(
            stockItemId,
            productId,
            normalizedUom,
            openingQuantity,
            key,
            sourceEventId));
        if (openingQuantity > 0)
        {
            aggregate.Receive(openingQuantity, idempotencyKey, correlationId, sourceEventId);
        }

        return aggregate;
    }

    public void Receive(decimal quantity, string idempotencyKey, string correlationId, Guid sourceEventId)
    {
        if (AlreadyProcessed(sourceEventId))
        {
            return;
        }

        Guard.Positive(quantity, "stock-receipt-quantity-invalid", "Receipt quantity");
        Raise(new StockReceived(CreateMovement(quantity, 0, "MANUAL", null, idempotencyKey, correlationId, sourceEventId)));
    }

    public void Adjust(decimal delta, string idempotencyKey, string correlationId, Guid sourceEventId)
    {
        if (AlreadyProcessed(sourceEventId))
        {
            return;
        }

        if (delta == 0)
        {
            throw new DomainException("stock-adjustment-quantity-invalid", "Adjustment delta cannot be zero.");
        }

        EnsureBalances(OnHandQuantity + delta, ReservedQuantity);
        Raise(new StockAdjusted(CreateMovement(delta, 0, "MANUAL", null, idempotencyKey, correlationId, sourceEventId)));
    }

    public void Deduct(decimal quantity, string idempotencyKey, string correlationId, Guid sourceEventId)
    {
        if (AlreadyProcessed(sourceEventId))
        {
            return;
        }

        Guard.Positive(quantity, "stock-deduction-quantity-invalid", "Deduction quantity");
        if (quantity > AvailableQuantity)
        {
            throw new ConflictException("insufficient-stock", "The deduction exceeds available stock.");
        }

        Raise(new StockDeducted(CreateMovement(-quantity, 0, "MANUAL", null, idempotencyKey, correlationId, sourceEventId)));
    }

    public void Reserve(decimal quantity, Guid referenceId, string correlationId, Guid sourceEventId)
    {
        if (AlreadyProcessed(sourceEventId))
        {
            if (_reservations.TryGetValue(referenceId, out decimal existingQuantity) && existingQuantity == quantity)
            {
                return;
            }

            throw new ConflictException(
                "stock-reservation-already-compensated",
                "The idempotent reservation was already completed and is no longer active.");
        }

        Guard.Positive(quantity, "stock-reservation-quantity-invalid", "Reservation quantity");
        if (referenceId == Guid.Empty)
        {
            throw new DomainException("stock-reference-id-required", "Reservation reference id is required.");
        }

        if (_reservations.ContainsKey(referenceId))
        {
            throw new ConflictException("stock-reservation-conflict", "A reservation already exists for this reference.");
        }

        if (quantity > AvailableQuantity)
        {
            throw new ConflictException("insufficient-stock", "The reservation exceeds available stock.");
        }

        Raise(new StockReserved(CreateMovement(0, quantity, "ORDER", referenceId, sourceEventId.ToString("N"), correlationId, sourceEventId)));
    }

    public void Commit(Guid referenceId, string correlationId, Guid sourceEventId)
    {
        if (AlreadyProcessed(sourceEventId))
        {
            return;
        }

        decimal quantity = GetReservation(referenceId);
        Raise(new StockCommitted(CreateMovement(-quantity, -quantity, "ORDER", referenceId, sourceEventId.ToString("N"), correlationId, sourceEventId)));
    }

    public void Release(Guid referenceId, string correlationId, Guid sourceEventId)
    {
        if (AlreadyProcessed(sourceEventId))
        {
            return;
        }

        decimal quantity = GetReservation(referenceId);
        Raise(new StockReleased(CreateMovement(0, -quantity, "ORDER", referenceId, sourceEventId.ToString("N"), correlationId, sourceEventId)));
    }

    public void ReleaseIfPresent(Guid referenceId, string correlationId, Guid sourceEventId)
    {
        if (AlreadyProcessed(sourceEventId) || !_reservations.TryGetValue(referenceId, out decimal quantity))
        {
            return;
        }

        Raise(new StockReleased(CreateMovement(0, -quantity, "ORDER", referenceId, sourceEventId.ToString("N"), correlationId, sourceEventId)));
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case StockItemOpened opened:
                Id = opened.StockItemId;
                ProductId = opened.ProductId;
                BaseUomCode = opened.BaseUomCode;
                break;
            case StockReserved reserved:
                ApplyMovement(reserved.Movement);
                _reservations.Add(reserved.Movement.ReferenceId!.Value, reserved.Movement.ReservedQuantityDelta);
                break;
            case StockCommitted committed:
                ApplyMovement(committed.Movement);
                _reservations.Remove(committed.Movement.ReferenceId!.Value);
                break;
            case StockReleased released:
                ApplyMovement(released.Movement);
                _reservations.Remove(released.Movement.ReferenceId!.Value);
                break;
            case IStockMovementEvent movementEvent:
                ApplyMovement(movementEvent.Movement);
                break;
            default:
                throw new InvalidOperationException($"Unsupported inventory event '{domainEvent.GetType().Name}'.");
        }
    }

    private bool AlreadyProcessed(Guid sourceEventId)
    {
        if (sourceEventId == Guid.Empty)
        {
            throw new DomainException("stock-source-event-id-required", "Source event id is required.");
        }

        return _processedSourceEvents.Contains(sourceEventId);
    }

    private decimal GetReservation(Guid referenceId)
    {
        if (!_reservations.TryGetValue(referenceId, out decimal quantity))
        {
            throw new ConflictException("stock-reservation-not-found", $"No active reservation exists for reference '{referenceId}'.");
        }

        return quantity;
    }

    private static StockMovementData CreateMovement(
        decimal onHandDelta,
        decimal reservedDelta,
        string referenceType,
        Guid? referenceId,
        string idempotencyKey,
        string correlationId,
        Guid sourceEventId)
    {
        string key = Guard.Required(
            idempotencyKey,
            "stock-idempotency-key-invalid",
            "Idempotency key",
            InventoryRules.IdempotencyKeyMaximumLength);
        string correlation = Guard.Required(
            correlationId,
            "stock-correlation-id-invalid",
            "Correlation id",
            InventoryRules.CorrelationIdMaximumLength);
        string reference = Guard.Required(
            referenceType,
            "stock-reference-type-invalid",
            "Reference type",
            InventoryRules.ReferenceTypeMaximumLength);
        if (sourceEventId == Guid.Empty)
        {
            throw new DomainException("stock-source-event-id-required", "Source event id is required.");
        }

        return new StockMovementData(
            Guid.CreateVersion7(),
            onHandDelta,
            reservedDelta,
            reference,
            referenceId,
            key,
            correlation,
            sourceEventId);
    }

    private void ApplyMovement(StockMovementData movement)
    {
        OnHandQuantity += movement.OnHandQuantityDelta;
        ReservedQuantity += movement.ReservedQuantityDelta;
        EnsureBalances(OnHandQuantity, ReservedQuantity);
        _processedSourceEvents.Add(movement.SourceEventId);
    }

    private static void EnsureBalances(decimal onHand, decimal reserved)
    {
        if (onHand < 0 || reserved < 0 || reserved > onHand)
        {
            throw new DomainException(
                "stock-balance-invalid",
                "Stock balances must satisfy on-hand >= reserved >= zero.");
        }
    }
}
