using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Logging.Application;
using Rudoger.Modules.Logging.Domain;

namespace Rudoger.Modules.Logging.Infrastructure;

public sealed class InternalCallLogger(
    IRequestLogStore store,
    ICorrelationContextAccessor correlationContextAccessor) : IInternalCallLogger
{
    public async Task<T> ExecuteAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        Guid id = await StartAsync(operation, cancellationToken);
        try
        {
            T result = await action(cancellationToken);
            await store.CompleteAsync(id, new RequestLogCompletion(200, "SUCCEEDED", null), cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            await store.CompleteAsync(
                id,
                new RequestLogCompletion(500, "FAILED", exception.GetType().FullName),
                CancellationToken.None);
            throw;
        }
    }

    public async Task ExecuteAsync(
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            operation,
            async token =>
            {
                await action(token);
                return true;
            },
            cancellationToken);
    }

    private Task<Guid> StartAsync(string operation, CancellationToken cancellationToken)
    {
        return store.StartAsync(
            new RequestLogStart(
                RequestLogChannel.Internal,
                correlationContextAccessor.Current.CorrelationId,
                null,
                null,
                operation,
                null,
                null),
            cancellationToken);
    }
}
