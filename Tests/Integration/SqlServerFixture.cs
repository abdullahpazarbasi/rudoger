using Testcontainers.MsSql;

namespace Rudoger.IntegrationTests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Rudoger_Test_Only_Strong_Password_123!")
        .Build();

    public IntegrationApiFactory Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        string connectionString = _container.GetConnectionString();
        Environment.SetEnvironmentVariable("ConnectionStrings__Rudoger", connectionString);
        Environment.SetEnvironmentVariable(
            "Jwt__Secret",
            "Rudoger_Integration_Tests_Secret_At_Least_32_Characters");
        Factory = new IntegrationApiFactory(connectionString);
        await Rudoger.Api.DatabaseLifecycle.MigrateAsync(Factory.Services, CancellationToken.None);
        await Rudoger.Api.DatabaseLifecycle.SeedAsync(Factory.Services, CancellationToken.None);
        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__Rudoger", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        await _container.DisposeAsync();
    }
}
