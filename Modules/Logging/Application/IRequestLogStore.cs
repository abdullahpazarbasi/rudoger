namespace Rudoger.Modules.Logging.Application;

public interface IRequestLogStore
{
    Task<Guid> StartAsync(RequestLogStart start, CancellationToken cancellationToken);

    Task CompleteAsync(Guid id, RequestLogCompletion completion, CancellationToken cancellationToken);
}
