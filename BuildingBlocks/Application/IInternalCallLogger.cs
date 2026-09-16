namespace Rudoger.BuildingBlocks.Application;

public interface IInternalCallLogger
{
    Task<T> ExecuteAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken);

    Task ExecuteAsync(
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken);
}
