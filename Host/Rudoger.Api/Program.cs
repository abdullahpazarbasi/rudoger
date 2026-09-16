using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Infrastructure;
using Rudoger.BuildingBlocks.Presentation;
using Rudoger.Modules.Authn.Infrastructure;
using Rudoger.Modules.Authn.Presentation;
using Rudoger.Modules.Inventory.Infrastructure;
using Rudoger.Modules.Inventory.Presentation;
using Rudoger.Modules.Logging.Infrastructure;
using Rudoger.Modules.Logging.Presentation;
using Rudoger.Modules.Order.Infrastructure;
using Rudoger.Modules.Order.Presentation;
using Rudoger.Modules.Product.Infrastructure;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.Api;

public sealed partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        string? requestedCommand = args.FirstOrDefault()?.ToLowerInvariant();
        bool hasExplicitCommand = requestedCommand is "migrate" or "seed" or "serve";
        string command = hasExplicitCommand ? requestedCommand! : "serve";
        string[] builderArguments = hasExplicitCommand ? args.Skip(1).ToArray() : args;
        WebApplication app = BuildApplication(builderArguments);

        switch (command)
        {
            case "migrate":
                await DatabaseLifecycle.MigrateAsync(app.Services, CancellationToken.None);
                return 0;
            case "seed":
                await DatabaseLifecycle.SeedAsync(app.Services, CancellationToken.None);
                return 0;
            case "serve":
                await app.RunAsync();
                return 0;
            default:
                throw new InvalidOperationException($"Unsupported application command '{command}'.");
        }
    }

    public static WebApplication BuildApplication(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Configuration
            .AddInMemoryCollection(DotEnvFile.Read(Path.Combine(builder.Environment.ContentRootPath, ".env")))
            .AddInMemoryCollection(DotEnvFile.Read(Path.Combine(builder.Environment.ContentRootPath, ".env.local")))
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        string connectionString = builder.Configuration.GetConnectionString("Rudoger")
            ?? throw new InvalidOperationException("Connection string 'Rudoger' is required.");

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        builder.Services.AddSingleton<EventTypeRegistry>(RudogerEventTypeRegistry.Create());
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi("v1");
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(TokensController).Assembly)
            .AddApplicationPart(typeof(ProductsController).Assembly)
            .AddApplicationPart(typeof(StockItemsController).Assembly)
            .AddApplicationPart(typeof(OrdersController).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problem = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://rudoger.dev/problems/invalid-request",
                    Title = "The request is invalid.",
                    Instance = context.HttpContext.Request.Path,
                };
                ICorrelationContextAccessor correlation = context.HttpContext.RequestServices
                    .GetRequiredService<ICorrelationContextAccessor>();
                problem.Extensions["correlationId"] = correlation.Current.CorrelationId;
                return new BadRequestObjectResult(problem)
                {
                    ContentTypes = { "application/problem+json" },
                };
            };
        });

        builder.Services.AddAuthnInfrastructure(builder.Configuration, connectionString);
        builder.Services.AddProductInfrastructure(connectionString);
        builder.Services.AddInventoryInfrastructure(connectionString);
        builder.Services.AddOrderInfrastructure(connectionString);
        builder.Services.AddRequestLoggingInfrastructure(connectionString);
        builder.Services.AddAuthnPresentation();
        builder.Services.AddProductPresentation();
        builder.Services.AddInventoryPresentation();
        builder.Services.AddOrderPresentation();
        builder.Services.AddLoggingPresentation();

        WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ApiRequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseStatusCodePages(async statusCodeContext =>
        {
            ICorrelationContextAccessor correlation = statusCodeContext.HttpContext.RequestServices
                .GetRequiredService<ICorrelationContextAccessor>();
            await StatusCodeProblemDetails.WriteAsync(
                statusCodeContext.HttpContext,
                correlation.Current.CorrelationId);
        });
        app.UseAuthorization();
        app.MapControllers();
        app.MapOpenApi("/openapi/{documentName}.json").AllowAnonymous();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteAsync,
        }).AllowAnonymous();
        return app;
    }
}
