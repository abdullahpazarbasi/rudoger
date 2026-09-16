using Rudoger.Modules.Logging.Domain;

namespace Rudoger.Modules.Logging.Application;

public sealed record RequestLogStart(
    RequestLogChannel Channel,
    string CorrelationId,
    string? Method,
    string? Path,
    string? Operation,
    string? RequestHeaders,
    string? RequestBody);
