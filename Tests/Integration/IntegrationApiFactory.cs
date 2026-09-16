using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rudoger.IntegrationTests;

public sealed class IntegrationApiFactory(string connectionString) : WebApplicationFactory<Rudoger.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Rudoger"] = connectionString,
                ["Jwt:Issuer"] = "rudoger-integration-tests",
                ["Jwt:Audience"] = "rudoger-integration-tests",
                ["Jwt:Secret"] = "Rudoger_Integration_Tests_Secret_At_Least_32_Characters",
                ["Jwt:LifetimeMinutes"] = "10",
            }));
        builder.ConfigureServices(services =>
        {
            ServiceDescriptor[] workers = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                .ToArray();
            foreach (ServiceDescriptor worker in workers)
            {
                services.Remove(worker);
            }
        });
    }
}
