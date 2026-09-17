using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Product.Application;
using Rudoger.Modules.Product.Domain;

namespace Rudoger.IntegrationTests;

/// <summary>
/// The event store writes the event envelopes on their own before the projections, so a lost version
/// race stays distinguishable from a projection constraint failure. Only the former is a concurrency
/// conflict; reporting the latter as one would tell a caller to retry a write that can never succeed.
/// </summary>
[Collection(SqlServerTestSuite.Name)]
public sealed class EventStoreConflictIntegrationTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task LosingTheVersionRaceIsReportedAsAConcurrencyConflict()
    {
        Guid productId = Guid.CreateVersion7();
        await SaveAsync(NewProduct(productId, Sku()));

        await using AsyncServiceScope first = fixture.Factory.Services.CreateAsyncScope();
        await using AsyncServiceScope second = fixture.Factory.Services.CreateAsyncScope();
        IProductRepository firstRepository = first.ServiceProvider.GetRequiredService<IProductRepository>();
        IProductRepository secondRepository = second.ServiceProvider.GetRequiredService<IProductRepository>();

        ProductAggregate firstCopy = await firstRepository.LoadAsync(productId, CancellationToken.None)
            ?? throw new InvalidOperationException("The product was not stored.");
        ProductAggregate secondCopy = await secondRepository.LoadAsync(productId, CancellationToken.None)
            ?? throw new InvalidOperationException("The product was not stored.");

        firstCopy.Change(Sku(), "First writer", 10m, "TRY");
        secondCopy.Change(Sku(), "Second writer", 20m, "TRY");

        await firstRepository.SaveAsync(firstCopy, CancellationToken.None);

        ConcurrencyException conflict = await Assert.ThrowsAsync<ConcurrencyException>(
            () => secondRepository.SaveAsync(secondCopy, CancellationToken.None));

        Assert.Equal(productId, conflict.AggregateId);
        Assert.DoesNotContain("Stream", conflict.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AProjectionConstraintFailureIsNotReportedAsAConcurrencyConflict()
    {
        string sharedSku = Sku();
        await SaveAsync(NewProduct(Guid.CreateVersion7(), sharedSku));

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        IProductRepository repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        // A different stream, so the event envelopes insert cleanly and only the unique SKU index fails.
        Exception? exception = await Record.ExceptionAsync(
            () => repository.SaveAsync(NewProduct(Guid.CreateVersion7(), sharedSku), CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsNotType<ConcurrencyException>(exception);
        Assert.IsType<DbUpdateException>(exception, exactMatch: false);
    }

    [Fact]
    public async Task AFailedProjectionLeavesNoEventBehind()
    {
        string sharedSku = Sku();
        await SaveAsync(NewProduct(Guid.CreateVersion7(), sharedSku));
        Guid rejectedId = Guid.CreateVersion7();

        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        IProductRepository repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        await Record.ExceptionAsync(
            () => repository.SaveAsync(NewProduct(rejectedId, sharedSku), CancellationToken.None));

        await using AsyncServiceScope verification = fixture.Factory.Services.CreateAsyncScope();
        IProductRepository verifier = verification.ServiceProvider.GetRequiredService<IProductRepository>();
        Assert.Null(await verifier.LoadAsync(rejectedId, CancellationToken.None));
    }

    private async Task SaveAsync(ProductAggregate aggregate)
    {
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        IProductRepository repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        await repository.SaveAsync(aggregate, CancellationToken.None);
    }

    private static ProductAggregate NewProduct(Guid id, string sku)
    {
        return ProductAggregate.Create(
            id,
            sku,
            "Conflict probe",
            "EA",
            5m,
            "TRY",
            [new PackagingDefinition(Guid.CreateVersion7(), 0, "EA", 1m, null, null, null, null, null)]);
    }

    private static string Sku()
    {
        return $"CONFLICT-{Guid.CreateVersion7():N}"[..32];
    }
}
