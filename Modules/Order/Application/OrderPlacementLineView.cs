namespace Rudoger.Modules.Order.Application;

public sealed record OrderPlacementLineView(int Num, Guid ProductId, string UomCode, decimal Quantity);
