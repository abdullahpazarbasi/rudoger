namespace Rudoger.Modules.Order.Application;

public sealed record OrderPlacementLineInput(Guid ProductId, string UomCode, decimal Quantity);
