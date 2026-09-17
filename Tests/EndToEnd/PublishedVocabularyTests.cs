using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Rudoger.EndToEndTests;

/// <summary>
/// What the HTTP API publishes must stay inside the vocabulary of the context that answers. These
/// tests drive the real API so a reintroduced leak fails here even if every unit test still passes.
/// The bearer token travels per request: the fixture's client is shared, and leaving a default
/// Authorization header on it would change what the other tests observe.
/// </summary>
[Collection(ApiTestSuite.Name)]
public sealed class PublishedVocabularyTests(RunningApiFixture fixture)
{
    [Fact]
    public async Task OrderPlacementFailuresUseTheOrderVocabulary()
    {
        string token = await AuthenticateAsync();
        Guid productId = await CreateProductAsync(token, $"vocab-{Guid.CreateVersion7():N}"[..24]);

        // No stock item was ever opened for this product, so the inventory context rejects the
        // reservation. The order API must describe that rejection in its own terms.
        using HttpResponseMessage placed = await SendAsync(
            HttpMethod.Post,
            "/api/v1/order/orders",
            token,
            new { lines = new[] { new { productId, uomCode = "EA", quantity = 1m } } },
            Guid.CreateVersion7().ToString("N"));
        Assert.Equal(HttpStatusCode.Accepted, placed.StatusCode);
        using JsonDocument placement = await ReadJsonAsync(placed);
        string placementId = placement.RootElement.GetProperty("id").GetString()!;

        JsonElement failed = await WaitForFailureAsync($"/api/v1/order/order-placements/{placementId}", token);
        string failureCode = failed.GetProperty("failureCode").GetString()!;
        string failureDetail = failed.GetProperty("failureDetail").GetString()!;

        Assert.StartsWith("order-", failureCode, StringComparison.Ordinal);
        foreach (string foreignToken in new[] { "stock-item", "insufficient-stock", "packaging-", "reservation" })
        {
            Assert.DoesNotContain(foreignToken, failureCode, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(foreignToken, failureDetail, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task TheOpenApiDocumentPublishesNoInternalNames()
    {
        using HttpResponseMessage response = await SendAsync(HttpMethod.Get, "/openapi/v1.json", token: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string document = await response.Content.ReadAsStringAsync();

        foreach (string leak in new[] { "sourceEventId", "streamId", "schemaVersion", "aggregateType", "View" })
        {
            Assert.DoesNotContain(leak, document, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task InvalidPagingReportsACodedProblemWithoutParameterNames()
    {
        string token = await AuthenticateAsync();

        using HttpResponseMessage response = await SendAsync(
            HttpMethod.Get,
            "/api/v1/product/products?pageNumber=0",
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal(
            "https://rudoger.dev/problems/paging-invalid",
            problem.RootElement.GetProperty("type").GetString());
        Assert.DoesNotContain(
            "Parameter",
            problem.RootElement.GetProperty("detail").GetString()!,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownResourcesReportACodedNotFoundProblem()
    {
        string token = await AuthenticateAsync();

        using HttpResponseMessage response = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/product/products/{Guid.CreateVersion7()}",
            token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal(
            "https://rudoger.dev/problems/product-not-found",
            problem.RootElement.GetProperty("type").GetString());
    }

    private async Task<string> AuthenticateAsync()
    {
        using HttpResponseMessage response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/authn/tokens",
            token: null,
            body: new { username = "murat", password = "12345678" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = await ReadJsonAsync(response);
        return payload.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<Guid> CreateProductAsync(string token, string sku)
    {
        using HttpResponseMessage response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/product/products",
            token,
            new
            {
                sku,
                name = "Vocabulary probe",
                baseUomCode = "EA",
                basePriceAmount = 1m,
                basePriceCurrencyCode = "TRY",
                packagings = new object[]
                {
                    new
                    {
                        id = (Guid?)null,
                        level = 0,
                        uomCode = "EA",
                        conversionFactor = 1m,
                        barcode = (string?)null,
                        weightInKg = (decimal?)null,
                        lengthInMm = (decimal?)null,
                        widthInMm = (decimal?)null,
                        heightInMm = (decimal?)null,
                    },
                },
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument product = await ReadJsonAsync(response);
        return product.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string? token,
        object? body = null,
        string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await fixture.Client.SendAsync(request);
    }

    private async Task<JsonElement> WaitForFailureAsync(string path, string token)
    {
        for (int attempt = 0; attempt < 60; attempt++)
        {
            using HttpResponseMessage response = await SendAsync(HttpMethod.Get, path, token);
            if (response.IsSuccessStatusCode)
            {
                using JsonDocument document = await ReadJsonAsync(response);
                if (document.RootElement.GetProperty("status").GetString() == "Failed")
                {
                    return document.RootElement.Clone();
                }
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"'{path}' never reached the Failed status.");
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }
}
