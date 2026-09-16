using Rudoger.Modules.Logging.Domain;

namespace Rudoger.Modules.Logging.Infrastructure;

public sealed class RequestLogEntity
{
    public Guid Id { get; set; }

    public RequestLogChannel Channel { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public string? Method { get; set; }

    public string? Path { get; set; }

    public string? Operation { get; set; }

    public string? RequestHeaders { get; set; }

    public string? RequestBody { get; set; }

    public int? StatusCode { get; set; }

    public string Outcome { get; set; } = "STARTED";

    public string? ProblemType { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public long? DurationInMilliseconds { get; set; }
}
