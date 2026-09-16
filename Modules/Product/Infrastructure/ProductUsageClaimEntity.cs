namespace Rudoger.Modules.Product.Infrastructure;

public sealed class ProductUsageClaimEntity
{
    public Guid ProductId { get; set; }

    public Guid OperationId { get; set; }

    public string UsageType { get; set; } = string.Empty;
}
