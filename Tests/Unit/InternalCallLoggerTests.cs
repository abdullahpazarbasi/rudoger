using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Logging.Application;
using Rudoger.Modules.Logging.Domain;
using Rudoger.Modules.Logging.Infrastructure;

namespace Rudoger.UnitTests;

public sealed class InternalCallLoggerTests
{
    [Fact]
    public async Task SucceededInternalCallsRecordNoHttpStatus()
    {
        var store = new RecordingRequestLogStore();
        InternalCallLogger logger = Create(store);

        int result = await logger.ExecuteAsync("Product.ClaimOrderUsage", _ => Task.FromResult(7), CancellationToken.None);

        Assert.Equal(7, result);
        Assert.Equal(RequestLogChannel.Internal, store.Start!.Channel);
        Assert.Null(store.Completion!.StatusCode);
        Assert.Equal("SUCCEEDED", store.Completion.Outcome);
        Assert.Null(store.Completion.ProblemType);
    }

    [Fact]
    public async Task FailedInternalCallsRecordTheFailureCodeInsteadOfAClrType()
    {
        var store = new RecordingRequestLogStore();
        InternalCallLogger logger = Create(store);

        await Assert.ThrowsAsync<ConflictException>(() => logger.ExecuteAsync(
            "Inventory.Reserve",
            _ => Task.FromException<bool>(new ConflictException("insufficient-stock", "Not enough stock.")),
            CancellationToken.None));

        Assert.Null(store.Completion!.StatusCode);
        Assert.Equal("FAILED", store.Completion.Outcome);
        Assert.Equal("insufficient-stock", store.Completion.ProblemType);
        Assert.DoesNotContain("Exception", store.Completion.ProblemType!, StringComparison.Ordinal);
        Assert.DoesNotContain("Rudoger.", store.Completion.ProblemType!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnexpectedFailuresRecordAStableCodeRatherThanTheClrTypeName()
    {
        var store = new RecordingRequestLogStore();
        InternalCallLogger logger = Create(store);

        await Assert.ThrowsAsync<InvalidOperationException>(() => logger.ExecuteAsync(
            "Inventory.Commit",
            _ => Task.FromException<bool>(new InvalidOperationException("boom")),
            CancellationToken.None));

        Assert.Equal("internal-call-failed", store.Completion!.ProblemType);
    }

    [Fact]
    public async Task VoidInternalCallsAreLoggedTheSameWay()
    {
        var store = new RecordingRequestLogStore();
        InternalCallLogger logger = Create(store);

        await logger.ExecuteAsync("Product.ReleaseOrderUsage", _ => Task.CompletedTask, CancellationToken.None);

        Assert.Null(store.Completion!.StatusCode);
        Assert.Equal("SUCCEEDED", store.Completion.Outcome);
    }

    private static InternalCallLogger Create(IRequestLogStore store)
    {
        var accessor = new CorrelationContextAccessor
        {
            Current = new CorrelationContext("correlation-id"),
        };
        return new InternalCallLogger(store, accessor);
    }

    private sealed class RecordingRequestLogStore : IRequestLogStore
    {
        public RequestLogStart? Start { get; private set; }

        public RequestLogCompletion? Completion { get; private set; }

        public Task<Guid> StartAsync(RequestLogStart start, CancellationToken cancellationToken)
        {
            Start = start;
            return Task.FromResult(Guid.CreateVersion7());
        }

        public Task CompleteAsync(Guid id, RequestLogCompletion completion, CancellationToken cancellationToken)
        {
            Completion = completion;
            return Task.CompletedTask;
        }
    }
}
