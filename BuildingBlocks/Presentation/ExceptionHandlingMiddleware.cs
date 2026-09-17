using Microsoft.AspNetCore.Mvc;
using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Domain;

namespace Rudoger.BuildingBlocks.Presentation;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly Action<ILogger, string, Exception?> LogUnhandledRequestFailure =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(LogUnhandledRequestFailure)),
            "Unhandled request failure with correlation id {CorrelationId}");

    public async Task InvokeAsync(HttpContext context, ICorrelationContextAccessor correlationContextAccessor)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (exception is DomainException or ConflictException or ConcurrencyException or NotFoundException or ValidationException or UnauthorizedAccessException)
        {
            await WriteKnownProblemAsync(context, correlationContextAccessor.Current.CorrelationId, exception);
        }
        catch (Exception exception)
        {
            LogUnhandledRequestFailure(logger, correlationContextAccessor.Current.CorrelationId, exception);
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "https://rudoger.dev/problems/unexpected-error",
                "An unexpected error occurred.",
                "The request could not be completed.",
                correlationContextAccessor.Current.CorrelationId);
        }
    }

    private static Task WriteKnownProblemAsync(HttpContext context, string correlationId, Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "https://rudoger.dev/problems/authentication-failed",
                "Authentication failed.",
                exception.Message,
                correlationId),
            ConflictException conflict => WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                $"https://rudoger.dev/problems/{conflict.Code}",
                "The requested operation conflicts with the current resource state.",
                conflict.Message,
                correlationId),
            DomainException domain => WriteProblemAsync(
                context,
                StatusCodes.Status422UnprocessableEntity,
                $"https://rudoger.dev/problems/{domain.Code}",
                "A business rule was violated.",
                domain.Message,
                correlationId),
            ConcurrencyException => WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "https://rudoger.dev/problems/concurrency-conflict",
                "A concurrency conflict occurred.",
                exception.Message,
                correlationId),
            NotFoundException notFound => WriteProblemAsync(
                context,
                StatusCodes.Status404NotFound,
                $"https://rudoger.dev/problems/{notFound.Code}",
                "The requested resource was not found.",
                notFound.Message,
                correlationId),
            ValidationException validation => WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                $"https://rudoger.dev/problems/{validation.Code}",
                "The request is invalid.",
                validation.Message,
                correlationId),
            _ => WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "https://rudoger.dev/problems/invalid-request",
                "The request is invalid.",
                exception.Message,
                correlationId),
        };
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string type,
        string title,
        string detail,
        string correlationId)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Type = type,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = correlationId },
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }
}
