using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Rudoger.EndToEndTests;

public sealed class RunningApiFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Rudoger_Test_Only_Strong_Password_123!")
        .Build();

    public WebApplication Application { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__Rudoger", _container.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Secret", "Rudoger_End_To_End_Secret_At_Least_32_Characters");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "EndToEndTests");
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:0");

        Application = Rudoger.Api.Program.BuildApplication([]);
        await Rudoger.Api.DatabaseLifecycle.MigrateAsync(Application.Services, CancellationToken.None);
        await Rudoger.Api.DatabaseLifecycle.SeedAsync(Application.Services, CancellationToken.None);
        await Application.StartAsync();

        IServer server = Application.Services.GetRequiredService<IServer>();
        string address = server.Features.Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("The test API address is unavailable.");
        Client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Application is not null)
        {
            await Application.StopAsync();
            await Application.DisposeAsync();
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__Rudoger", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        await _container.DisposeAsync();
    }
}
