namespace Rudoger.Modules.Order.Domain;

public static class OrderRules
{
    public const int OrderNumberMaximumLength = 64;
    public const int UomCodeMaximumLength = 16;
    public const int CurrencyCodeLength = 3;
    public const int IdempotencyKeyMaximumLength = 128;
    public const int FailureCodeMaximumLength = 100;
    public const int FailureDetailMaximumLength = 1000;
    public const int MaximumLines = 100;
}
