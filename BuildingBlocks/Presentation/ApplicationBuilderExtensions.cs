namespace Rudoger.BuildingBlocks.Presentation;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRudogerRequestInfrastructure(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app
            .UseMiddleware<CorrelationIdMiddleware>()
            .UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
