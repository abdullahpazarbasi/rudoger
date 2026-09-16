using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rudoger.Api;
using Rudoger.Modules.Authn.Infrastructure;
using Rudoger.Modules.Inventory.Infrastructure;
using Rudoger.Modules.Logging.Infrastructure;
using Rudoger.Modules.Order.Infrastructure;
using Rudoger.Modules.Product.Infrastructure;

namespace Rudoger.IntegrationTests;

[Collection(SqlServerTestSuite.Name)]
public sealed class PersistenceIntegrationTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task HealthEndpointReportsApplicationDatabaseConnectivity()
    {
        using HttpResponseMessage response = await fixture.Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("Healthy", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "Healthy",
            payload.RootElement.GetProperty("checks").GetProperty("database").GetProperty("status").GetString());
    }

    [Fact]
    public async Task HealthCheckReportsUnavailableApplicationDatabase()
    {
        const string unavailableConnectionString =
            "Server=127.0.0.1,1;Database=Rudoger;User Id=sa;Password=Rudoger_Unavailable_123!;"
            + "Encrypt=False;Connect Timeout=1";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AuthnDbContext>(options => options.UseSqlServer(unavailableConnectionString));
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        HealthCheckService healthChecks = serviceProvider.GetRequiredService<HealthCheckService>();

        HealthReport report = await healthChecks.CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal(HealthStatus.Unhealthy, report.Entries["database"].Status);
    }

    [Fact]
    public async Task MigrationsCreateIsolatedSchemasAndSeedUsers()
    {
        await using AsyncServiceScope scope = fixture.Factory.Services.CreateAsyncScope();
        AuthnDbContext authn = scope.ServiceProvider.GetRequiredService<AuthnDbContext>();
        ProductDbContext product = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        InventoryDbContext inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        OrderDbContext order = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        LoggingDbContext logging = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();

        Assert.Equal(3, await authn.Users.CountAsync());
        Assert.True(await authn.Database.CanConnectAsync());
        Assert.True(await product.Database.CanConnectAsync());
        Assert.True(await inventory.Database.CanConnectAsync());
        Assert.True(await order.Database.CanConnectAsync());
        Assert.True(await logging.Database.CanConnectAsync());

        string[] schemas = await authn.Database.SqlQueryRaw<string>(
                "SELECT [name] AS [Value] FROM sys.schemas WHERE [name] IN ('authn','product','inventory','order','logging')")
            .ToArrayAsync();
        Assert.Equal(5, schemas.Length);
    }
}
