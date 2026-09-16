using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Logging.Application;
using Rudoger.Modules.Logging.Domain;

namespace Rudoger.Modules.Logging.Presentation;

public sealed class ApiRequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<ApiRequestLoggingMiddleware> logger)
{
    private const int MaximumLoggedBodyLength = 1_048_576;

    private static readonly Action<ILogger, string, Exception?> LogPersistenceFailure =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(LogPersistenceFailure)),
            "Request log persistence failed for correlation id {CorrelationId}");

    public async Task InvokeAsync(
        HttpContext context,
        IRequestLogStore store,
        SensitiveDataMasker masker,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        string correlationId = correlationContextAccessor.Current.CorrelationId;
        Guid logId;
        try
        {
            string? body = await ReadBodyAsync(context.Request, masker, context.RequestAborted);
            logId = await store.StartAsync(
                new RequestLogStart(
                    RequestLogChannel.Http,
                    correlationId,
                    context.Request.Method,
                    masker.MaskPathAndQuery(context.Request),
                    null,
                    masker.MaskHeaders(context.Request.Headers),
                    body),
                context.RequestAborted);
        }
        catch (Exception exception)
        {
            LogPersistenceFailure(logger, correlationId, exception);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Type = "https://rudoger.dev/problems/request-logging-unavailable",
                    Title = "Request logging is unavailable.",
                    Detail = "The request cannot be accepted because its mandatory audit record could not be created.",
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Instance = context.Request.Path,
                    Extensions = { ["correlationId"] = correlationId },
                },
                options: null,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted);
            return;
        }

        await next(context);

        try
        {
            await store.CompleteAsync(
                logId,
                new RequestLogCompletion(
                    context.Response.StatusCode,
                    context.Response.StatusCode < 400 ? "SUCCEEDED" : "FAILED",
                    ProblemTypeFor(context.Response.StatusCode)),
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            LogPersistenceFailure(logger, correlationId, exception);
        }
    }

    private static string? ProblemTypeFor(int statusCode)
    {
        return statusCode < 400 ? null : statusCode switch
        {
            StatusCodes.Status400BadRequest => "https://rudoger.dev/problems/invalid-request",
            StatusCodes.Status401Unauthorized => "https://rudoger.dev/problems/authentication-required",
            StatusCodes.Status403Forbidden => "https://rudoger.dev/problems/access-forbidden",
            StatusCodes.Status404NotFound => "https://rudoger.dev/problems/resource-not-found",
            StatusCodes.Status409Conflict => "https://rudoger.dev/problems/conflict",
            StatusCodes.Status415UnsupportedMediaType => "https://rudoger.dev/problems/unsupported-media-type",
            StatusCodes.Status422UnprocessableEntity => "https://rudoger.dev/problems/business-rule-violation",
            _ => "https://rudoger.dev/problems/http-error",
        };
    }

    private static async Task<string?> ReadBodyAsync(
        HttpRequest request,
        SensitiveDataMasker masker,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength == 0)
        {
            return null;
        }

        if (request.ContentLength > MaximumLoggedBodyLength)
        {
            return "[BODY OMITTED: SIZE LIMIT EXCEEDED]";
        }

        request.EnableBuffering();
        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        char[] buffer = new char[MaximumLoggedBodyLength + 1];
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await reader.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        request.Body.Position = 0;
        if (totalRead > MaximumLoggedBodyLength)
        {
            return "[BODY OMITTED: SIZE LIMIT EXCEEDED]";
        }

        if (totalRead == 0)
        {
            return null;
        }

        string body = new(buffer, 0, totalRead);
        return request.HasJsonContentType() ? masker.MaskJson(body) : "[NON-JSON BODY OMITTED]";
    }
}
