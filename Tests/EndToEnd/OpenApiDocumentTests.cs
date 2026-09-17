using System.Net;
using System.Text;
using System.Text.Json;

namespace Rudoger.EndToEndTests;

/// <summary>
/// The OpenAPI document is the only interactive client the repository ships, via Swagger UI. These
/// tests pin what that client needs to walk the API unaided: a bearer scheme on exactly the
/// operations the authorization middleware protects, and request examples the API accepts as-is.
/// </summary>
[Collection(ApiTestSuite.Name)]
public sealed class OpenApiDocumentTests(RunningApiFixture fixture)
{
    [Fact]
    public async Task ProtectedOperationsRequireTheDeclaredBearerScheme()
    {
        using JsonDocument document = await ReadDocumentAsync();
        JsonElement scheme = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());

        JsonElement paths = document.RootElement.GetProperty("paths");
        JsonElement tokenExchange = paths.GetProperty("/api/v1/authn/tokens").GetProperty("post");
        Assert.False(tokenExchange.TryGetProperty("security", out _), "Token exchange must stay anonymous.");

        foreach (string path in new[] { "/api/v1/product/products", "/api/v1/inventory/stock-items", "/api/v1/order/orders" })
        {
            foreach (JsonProperty operation in paths.GetProperty(path).EnumerateObject())
            {
                JsonElement requirement = Assert.Single(operation.Value.GetProperty("security").EnumerateArray());
                Assert.True(requirement.TryGetProperty("Bearer", out _), $"{operation.Name} {path} lacks the bearer requirement.");
            }
        }
    }

    [Fact]
    public async Task RequestSchemasPublishExamplesTheApiAccepts()
    {
        using JsonDocument document = await ReadDocumentAsync();
        JsonElement schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        foreach (string schema in new[]
                 {
                     "TokenExchangeRequest",
                     "CreateProductRequest",
                     "CreateStockItemRequest",
                     "CreateStockMovementRequest",
                     "CreateOrderRequest",
                     "CreateOrderTransitionRequest",
                 })
        {
            Assert.True(
                schemas.GetProperty(schema).TryGetProperty("examples", out JsonElement examples) && examples.GetArrayLength() == 1,
                $"'{schema}' publishes no example.");
        }

        // Every Swagger UI session starts from the token example, so it must authenticate unchanged.
        JsonElement tokenExample = schemas.GetProperty("TokenExchangeRequest").GetProperty("examples")[0];
        using var content = new StringContent(tokenExample.GetRawText(), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await fixture.Client.PostAsync("/api/v1/authn/tokens", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<JsonDocument> ReadDocumentAsync()
    {
        using HttpResponseMessage response = await fixture.Client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }
}
