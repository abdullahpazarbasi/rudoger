using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Inventory.Application;

namespace Rudoger.Modules.Inventory.Infrastructure;

public sealed class InventoryOutboxWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<InventoryOutboxWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Guid, string, Exception?> LogReleaseFailure =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogReleaseFailure)),
            "Inventory outbox message {MessageId} failed: {Error}");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            InventoryOutboxMessageEntity? message = await TryClaimAsync(stoppingToken);
            if (message is null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                continue;
            }

            try
            {
                await ReleaseAsync(message, stoppingToken);
                await MarkProcessedAsync(message.Id, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogReleaseFailure(logger, message.Id, exception.Message, exception);
                await MarkFailedAsync(message.Id, exception.Message, stoppingToken);
            }
        }
    }

    private async Task<InventoryOutboxMessageEntity?> TryClaimAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        InventoryDbContext dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        DateTimeOffset now = timeProvider.GetUtcNow();
        InventoryOutboxMessageEntity? candidate = await dbContext.OutboxMessages.AsNoTracking()
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

    private async Task ReleaseAsync(InventoryOutboxMessageEntity message, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ICorrelationContextAccessor correlation = scope.ServiceProvider.GetRequiredService<ICorrelationContextAccessor>();
        correlation.Current = new CorrelationContext(message.Id.ToString("N"), CausationId: message.Id);
        IProductInventoryGateway gateway = scope.ServiceProvider.GetRequiredService<IProductInventoryGateway>();
        await gateway.ReleaseUsageAsync(message.ProductId, message.OperationId, cancellationToken);
    }

    private async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        InventoryDbContext dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.OutboxMessages.Where(item => item.Id == id).ExecuteUpdateAsync(
            setters => setters
                .SetProperty(item => item.ProcessedAtUtc, timeProvider.GetUtcNow())
                .SetProperty(item => item.LockedUntilUtc, (DateTimeOffset?)null)
                .SetProperty(item => item.LastError, (string?)null),
            cancellationToken);
    }

    private async Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        InventoryDbContext dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        InventoryOutboxMessageEntity message = await dbContext.OutboxMessages.SingleAsync(
            item => item.Id == id,
            cancellationToken);
        int delaySeconds = Math.Min(60, 1 << Math.Min(message.Attempts, 6));
        message.LockedUntilUtc = null;
        message.NextAttemptAtUtc = timeProvider.GetUtcNow().AddSeconds(delaySeconds);
        message.LastError = error.Length <= 2000 ? error : error[..2000];
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
