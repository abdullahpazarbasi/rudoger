namespace Rudoger.Modules.Logging.Domain;

public static class RequestLogRules
{
    public const int CorrelationIdMaximumLength = 128;
    public const int MethodMaximumLength = 20;
    public const int PathMaximumLength = 2048;
    public const int OperationMaximumLength = 200;
    public const int ProblemTypeMaximumLength = 500;
}
