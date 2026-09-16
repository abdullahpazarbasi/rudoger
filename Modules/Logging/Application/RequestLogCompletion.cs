namespace Rudoger.Modules.Logging.Application;

public sealed record RequestLogCompletion(int StatusCode, string Outcome, string? ProblemType);
