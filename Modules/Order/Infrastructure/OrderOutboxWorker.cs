using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Infrastructure;

public sealed class OrderOutboxWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OrderOutboxWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Guid, string, Exception?> LogWorkflowFailure =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogWorkflowFailure)),
            "Order workflow message {MessageId} failed: {Error}");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            OrderOutboxMessageEntity? message = await TryClaimAsync(stoppingToken);
            if (message is null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                continue;
            }

            try
            {
                await ProcessAsync(message, stoppingToken);
                await MarkProcessedAsync(message.Id, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogWorkflowFailure(logger, message.Id, exception.Message, exception);
                await MarkFailedAsync(message.Id, exception.Message, stoppingToken);
            }
        }
    }

    private async Task<OrderOutboxMessageEntity?> TryClaimAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        OrderDbContext dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        DateTimeOffset now = timeProvider.GetUtcNow();
        OrderOutboxMessageEntity? candidate = await dbContext.OutboxMessages.AsNoTracking()
            .Where(item => item.ProcessedAtUtc == null
                && item.NextAttemptAtUtc <= now
                && (item.LockedUntilUtc == null || item.LockedUntilUtc < now))
            .OrderBy(item => item.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (candidate is null)
        {
            return null;
        }

        int changed = await dbContext.OutboxMessages
            .Where(item => item.Id == candidate.Id
                && item.ProcessedAtUtc == null
                && (item.LockedUntilUtc == null || item.LockedUntilUtc < now))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.LockedUntilUtc, now.AddMinutes(1))
                    .SetProperty(item => item.Attempts, item => item.Attempts + 1),
                cancellationToken);
        return changed == 1 ? candidate : null;
    }

    private async Task ProcessAsync(OrderOutboxMessageEntity message, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ICorrelationContextAccessor correlation = scope.ServiceProvider.GetRequiredService<ICorrelationContextAccessor>();
        correlation.Current = new CorrelationContext(message.Id.ToString("N"), CausationId: message.Id);
        OrderWorkflowService workflow = scope.ServiceProvider.GetRequiredService<OrderWorkflowService>();
        switch (message.Type)
        {
            case OrderWorkflowMessageType.ProcessPlacement:
                await workflow.ProcessPlacementAsync(message.AggregateId, cancellationToken);
                break;
            case OrderWorkflowMessageType.ProcessTransition:
                await workflow.ProcessTransitionAsync(
                    message.AggregateId,
                    message.SecondaryId ?? throw new InvalidOperationException("Transition id is missing."),
                    cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unsupported workflow message type '{message.Type}'.");
        }
    }

    private async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        OrderDbContext dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        DateTimeOffset now = timeProvider.GetUtcNow();
        await dbContext.OutboxMessages.Where(item => item.Id == messageId).ExecuteUpdateAsync(
            setters => setters
                .SetProperty(item => item.ProcessedAtUtc, now)
                .SetProperty(item => item.LockedUntilUtc, (DateTimeOffset?)null)
                .SetProperty(item => item.LastError, (string?)null),
            cancellationToken);
    }

    private async Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        OrderDbContext dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        OrderOutboxMessageEntity message = await dbContext.OutboxMessages.SingleAsync(
            item => item.Id == messageId,
            cancellationToken);
        int delaySeconds = Math.Min(60, 1 << Math.Min(message.Attempts, 6));
        message.LockedUntilUtc = null;
        message.NextAttemptAtUtc = timeProvider.GetUtcNow().AddSeconds(delaySeconds);
        message.LastError = error.Length <= 2000 ? error : error[..2000];
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
