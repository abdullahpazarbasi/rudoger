using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Logging.Application;
using Rudoger.Modules.Logging.Domain;

namespace Rudoger.Modules.Logging.Infrastructure;

public sealed class InternalCallLogger(
    IRequestLogStore store,
    ICorrelationContextAccessor correlationContextAccessor) : IInternalCallLogger
{
    private const string UnexpectedFailureCode = "internal-call-failed";

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
            await store.CompleteAsync(id, new RequestLogCompletion(null, "SUCCEEDED", null), cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            await store.CompleteAsync(
                id,
                new RequestLogCompletion(null, "FAILED", FailureCodeOf(exception)),
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

    // The audit record carries the failure code the raising context published, never a CLR type name:
    // an implementation type has no place in a field that otherwise holds a problem identifier.
    private static string FailureCodeOf(Exception exception)
    {
        return exception switch
        {
            DomainException domain => domain.Code,
            ConflictException conflict => conflict.Code,
            NotFoundException notFound => notFound.Code,
            ValidationException validation => validation.Code,
            _ => UnexpectedFailureCode,
        };
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
