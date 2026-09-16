using Rudoger.BuildingBlocks.Application;

namespace Rudoger.BuildingBlocks.Presentation;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ICorrelationContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(context);
        string requested = context.Request.Headers[HeaderName].ToString().Trim();
        string correlationId = IsValid(requested) ? requested : Guid.CreateVersion7().ToString("N");
        Guid? userId = Guid.TryParse(context.User.FindFirst("sub")?.Value, out Guid parsed) ? parsed : null;

        accessor.Current = new CorrelationContext(correlationId, userId);
        context.Response.Headers[HeaderName] = correlationId;
        await next(context);
    }

    private static bool IsValid(string value)
    {
        return value.Length is > 0 and <= 128 && value.All(character => character is >= (char)33 and <= (char)126);
    }
}
