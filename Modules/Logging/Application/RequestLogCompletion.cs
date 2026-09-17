namespace Rudoger.Modules.Logging.Application;

/// <summary>
/// The outcome of a logged request. <paramref name="StatusCode"/> is null for channels that have no
/// HTTP status, such as internal calls between bounded contexts.
/// </summary>
public sealed record RequestLogCompletion(int? StatusCode, string Outcome, string? ProblemType);
