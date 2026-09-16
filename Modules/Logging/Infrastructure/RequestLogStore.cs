using Microsoft.EntityFrameworkCore;
using Rudoger.Modules.Logging.Application;

namespace Rudoger.Modules.Logging.Infrastructure;

public sealed class RequestLogStore(LoggingDbContext dbContext, TimeProvider timeProvider) : IRequestLogStore
{
    public async Task<Guid> StartAsync(RequestLogStart start, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(start);
        Guid id = Guid.CreateVersion7();
        dbContext.RequestLogs.Add(new RequestLogEntity
        {
            Id = id,
            Channel = start.Channel,
            CorrelationId = start.CorrelationId,
            Method = start.Method,
            Path = start.Path,
            Operation = start.Operation,
            RequestHeaders = start.RequestHeaders,
            RequestBody = start.RequestBody,
            StartedAtUtc = timeProvider.GetUtcNow(),
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return id;
    }

    public async Task CompleteAsync(Guid id, RequestLogCompletion completion, CancellationToken cancellationToken)
    {
        RequestLogEntity entity = await dbContext.RequestLogs.SingleAsync(item => item.Id == id, cancellationToken);
        DateTimeOffset completedAt = timeProvider.GetUtcNow();
        entity.StatusCode = completion.StatusCode;
        entity.Outcome = completion.Outcome;
        entity.ProblemType = completion.ProblemType;
        entity.CompletedAtUtc = completedAt;
        entity.DurationInMilliseconds = Math.Max(0, (long)(completedAt - entity.StartedAtUtc).TotalMilliseconds);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
