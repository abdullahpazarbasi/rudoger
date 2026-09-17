namespace Rudoger.Modules.Order.Domain;

public sealed record OrderPlacementLine(
    Guid Id,
    int Num,
    Guid ProductId,
    string UomCode,
    decimal Quantity,
    Guid ReservationOperationId,
    Guid ReleaseOperationId);
