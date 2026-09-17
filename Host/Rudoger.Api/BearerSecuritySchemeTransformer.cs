using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Rudoger.Api;

/// <summary>
/// Declares the JWT bearer scheme so OpenAPI clients such as Swagger UI can attach the token that the
/// protected controllers require. Which operations require it is decided per operation by
/// <see cref="BearerSecurityRequirementTransformer"/>.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Access token issued by POST /api/v1/authn/tokens.",
        };
        return Task.CompletedTask;
    }
}
