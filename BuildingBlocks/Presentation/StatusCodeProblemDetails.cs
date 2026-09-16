using Microsoft.AspNetCore.Mvc;

namespace Rudoger.BuildingBlocks.Presentation;

public static class StatusCodeProblemDetails
{
    public static Task WriteAsync(HttpContext context, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(context);
        (string type, string title, string detail) = context.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => (
                "https://rudoger.dev/problems/authentication-required",
                "Authentication is required.",
                "A valid bearer token is required to access this resource."),
            StatusCodes.Status403Forbidden => (
                "https://rudoger.dev/problems/access-forbidden",
                "Access is forbidden.",
                "The authenticated principal is not allowed to access this resource."),
            StatusCodes.Status404NotFound => (
                "https://rudoger.dev/problems/resource-not-found",
                "The requested resource was not found.",
                "No endpoint or resource matched the request."),
            StatusCodes.Status405MethodNotAllowed => (
                "https://rudoger.dev/problems/method-not-allowed",
                "The HTTP method is not allowed.",
                "The requested resource does not support this HTTP method."),
            StatusCodes.Status415UnsupportedMediaType => (
                "https://rudoger.dev/problems/unsupported-media-type",
                "The media type is not supported.",
                "Use one of the media types supported by this endpoint."),
            _ => (
                "https://rudoger.dev/problems/http-error",
                "The request could not be completed.",
                "The server returned an unsuccessful HTTP status code."),
        };
        var problem = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Type = type,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = correlationId },
        };
        return context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }
}
