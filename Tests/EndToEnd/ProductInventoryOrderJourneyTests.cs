using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rudoger.Modules.Logging.Infrastructure;

namespace Rudoger.EndToEndTests;

[Collection(ApiTestSuite.Name)]
public sealed class ProductInventoryOrderJourneyTests(RunningApiFixture fixture)
{
    [Fact]
    public async Task AuthenticatedJourneyPreservesStockAndAuditInvariants()
    {
        using HttpResponseMessage openApi = await fixture.Client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        Assert.Contains(
            "/api/v1/product/products",
            await openApi.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);

        using HttpResponseMessage unauthorized = await fixture.Client.GetAsync("/api/v1/product/products");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal("application/problem+json", unauthorized.Content.Headers.ContentType?.MediaType);
        using JsonDocument unauthorizedProblem = await ReadJsonAsync(unauthorized);
        Assert.Equal(
            "https://rudoger.dev/problems/authentication-required",
            unauthorizedProblem.RootElement.GetProperty("type").GetString());
        Assert.True(unauthorized.Headers.Contains("X-Correlation-Id"));

        using HttpResponseMessage tokenResponse = await fixture.Client.PostAsJsonAsync(
            "/api/v1/authn/tokens",
            new { username = "abdullah", password = "12345678" });
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        using JsonDocument token = await ReadJsonAsync(tokenResponse);
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token.RootElement.GetProperty("accessToken").GetString());

        using HttpResponseMessage productResponse = await fixture.Client.PostAsJsonAsync(
            "/api/v1/product/products",
            new
            {
                sku = "coffee-001",
                name = "Coffee",
                baseUomCode = "EA",
                basePriceAmount = 2.50m,
                basePriceCurrencyCode = "TRY",
                packagings = new object[]
                {
                    new
                    {
                        id = (Guid?)null,
                        level = 0,
                        uomCode = "EA",
                        conversionFactor = 1m,
                        barcode = "869000000001",
                        weightInKg = (decimal?)null,
                        lengthInMm = (decimal?)null,
                        widthInMm = (decimal?)null,
                        heightInMm = (decimal?)null,
                    },
                    new
                    {
                        id = (Guid?)null,
                        level = 1,
                        uomCode = "CASE",
                        conversionFactor = 10m,
                        barcode = "869000000010",
                        weightInKg = 1m,
                        lengthInMm = 100m,
                        widthInMm = 100m,
                        heightInMm = 100m,
                    },
                },
            });
        string productPayload = await productResponse.Content.ReadAsStringAsync();
        Assert.True(
            productResponse.StatusCode == HttpStatusCode.Created,
            $"Product creation returned {(int)productResponse.StatusCode}: {productPayload}");
        using JsonDocument product = JsonDocument.Parse(productPayload);
        Guid productId = product.RootElement.GetProperty("id").GetGuid();

        const string stockIdempotencyKey = "stock-open-coffee-001";
        using HttpResponseMessage stockResponse = await SendWithIdempotencyKeyAsync(
            "/api/v1/inventory/stock-items",
            new { productId, uomCode = "CASE", openingQuantity = 10m },
            stockIdempotencyKey);
        Assert.Equal(HttpStatusCode.Created, stockResponse.StatusCode);
        using JsonDocument stock = await ReadJsonAsync(stockResponse);
        Guid stockItemId = stock.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(100m, stock.RootElement.GetProperty("availableQuantity").GetDecimal());

        using HttpResponseMessage stockRetry = await SendWithIdempotencyKeyAsync(
            "/api/v1/inventory/stock-items",
            new { productId, uomCode = "case", openingQuantity = 10m },
            stockIdempotencyKey);
        Assert.Equal(HttpStatusCode.Created, stockRetry.StatusCode);
        using JsonDocument retriedStock = await ReadJsonAsync(stockRetry);
        Assert.Equal(stockItemId, retriedStock.RootElement.GetProperty("id").GetGuid());

        const string receiptIdempotencyKey = "stock-receipt-coffee-001";
        using HttpResponseMessage receiptResponse = await SendWithIdempotencyKeyAsync(
            $"/api/v1/inventory/stock-items/{stockItemId}/movements",
            new { type = "Receipt", uomCode = "CASE", quantity = 2m },
            receiptIdempotencyKey);
        Assert.Equal(HttpStatusCode.Created, receiptResponse.StatusCode);
        using JsonDocument receipt = await ReadJsonAsync(receiptResponse);
        Guid receiptId = receipt.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("CASE", receipt.RootElement.GetProperty("uomCode").GetString());
        Assert.Equal(2m, receipt.RootElement.GetProperty("quantity").GetDecimal());
        Assert.Equal(20m, receipt.RootElement.GetProperty("onHandQuantityDelta").GetDecimal());

        using HttpResponseMessage receiptRetryResponse = await SendWithIdempotencyKeyAsync(
            $"/api/v1/inventory/stock-items/{stockItemId}/movements",
            new { type = "Receipt", uomCode = "case", quantity = 2m },
            receiptIdempotencyKey);
        Assert.Equal(HttpStatusCode.Created, receiptRetryResponse.StatusCode);
        using JsonDocument receiptRetry = await ReadJsonAsync(receiptRetryResponse);
        Assert.Equal(receiptId, receiptRetry.RootElement.GetProperty("id").GetGuid());

        using HttpResponseMessage placementResponse = await SendWithIdempotencyKeyAsync(
            "/api/v1/order/orders",
            new { lines = new[] { new { productId, uomCode = "CASE", quantity = 2m } } },
            "order-coffee-001");
        Assert.Equal(HttpStatusCode.Accepted, placementResponse.StatusCode);
        using JsonDocument placement = await ReadJsonAsync(placementResponse);
        Guid placementId = placement.RootElement.GetProperty("id").GetGuid();
        Guid orderId = placement.RootElement.GetProperty("orderId").GetGuid();

        using HttpResponseMessage placementRetryResponse = await SendWithIdempotencyKeyAsync(
            "/api/v1/order/orders",
            new { lines = new[] { new { productId, uomCode = "case", quantity = 2m } } },
            "order-coffee-001");
        Assert.Equal(HttpStatusCode.Accepted, placementRetryResponse.StatusCode);
        using JsonDocument placementRetry = await ReadJsonAsync(placementRetryResponse);
        Assert.Equal(placementId, placementRetry.RootElement.GetProperty("id").GetGuid());

        using HttpResponseMessage placementConflictResponse = await SendWithIdempotencyKeyAsync(
            "/api/v1/order/orders",
            new { lines = new[] { new { productId, uomCode = "CASE", quantity = 3m } } },
            "order-coffee-001");
        Assert.Equal(HttpStatusCode.Conflict, placementConflictResponse.StatusCode);

        await WaitForPropertyAsync(
            $"/api/v1/order/order-placements/{placementId}",
            "status",
            "Succeeded");

        using HttpResponseMessage reservedStockResponse = await fixture.Client.GetAsync(
            $"/api/v1/inventory/stock-items/{stockItemId}");
        using JsonDocument reservedStock = await ReadJsonAsync(reservedStockResponse);
        Assert.Equal(120m, reservedStock.RootElement.GetProperty("onHandQuantity").GetDecimal());
        Assert.Equal(20m, reservedStock.RootElement.GetProperty("reservedQuantity").GetDecimal());
        Assert.Equal(100m, reservedStock.RootElement.GetProperty("availableQuantity").GetDecimal());

        using HttpResponseMessage orderResponse = await fixture.Client.GetAsync($"/api/v1/order/orders/{orderId}");
        using JsonDocument order = await ReadJsonAsync(orderResponse);
        JsonElement orderLine = order.RootElement.GetProperty("lines")[0];
        Assert.Equal(25m, orderLine.GetProperty("unitPriceAmount").GetDecimal());
        Assert.Equal("TRY", orderLine.GetProperty("unitPriceCurrencyCode").GetString());

        using HttpResponseMessage transitionResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/v1/order/orders/{orderId}/transitions",
            new { target = "Shipped" });
        Assert.Equal(HttpStatusCode.Accepted, transitionResponse.StatusCode);
        await WaitForPropertyAsync($"/api/v1/order/orders/{orderId}", "status", "Shipped");

        using HttpResponseMessage committedStockResponse = await fixture.Client.GetAsync(
            $"/api/v1/inventory/stock-items/{stockItemId}");
        using JsonDocument committedStock = await ReadJsonAsync(committedStockResponse);
        Assert.Equal(100m, committedStock.RootElement.GetProperty("onHandQuantity").GetDecimal());
        Assert.Equal(0m, committedStock.RootElement.GetProperty("reservedQuantity").GetDecimal());
        Assert.Equal(100m, committedStock.RootElement.GetProperty("availableQuantity").GetDecimal());

        using HttpResponseMessage deleteResponse = await fixture.Client.DeleteAsync($"/api/v1/product/products/{productId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        Assert.Equal("application/problem+json", deleteResponse.Content.Headers.ContentType?.MediaType);

        await using AsyncServiceScope scope = fixture.Application.Services.CreateAsyncScope();
        LoggingDbContext logging = scope.ServiceProvider.GetRequiredService<LoggingDbContext>();
        string[] loggedBodies = await logging.RequestLogs.AsNoTracking()
            .Where(item => item.RequestBody != null)
            .Select(item => item.RequestBody!)
            .ToArrayAsync();
        string[] loggedHeaders = await logging.RequestLogs.AsNoTracking()
            .Where(item => item.RequestHeaders != null)
            .Select(item => item.RequestHeaders!)
            .ToArrayAsync();
        Assert.DoesNotContain(loggedBodies, body => body.Contains("12345678", StringComparison.Ordinal));
        Assert.Contains(loggedBodies, body => body.Contains("***MASKED***", StringComparison.Ordinal));
        Assert.DoesNotContain(loggedHeaders, headers => headers.Contains(token.RootElement.GetProperty("accessToken").GetString()!, StringComparison.Ordinal));
        Assert.Contains(await logging.RequestLogs.ToArrayAsync(), item => item.Channel.ToString() == "Internal");
    }

    private async Task<HttpResponseMessage> SendWithIdempotencyKeyAsync<T>(string path, T body, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("Idempotency-Key", key);
        return await fixture.Client.SendAsync(request);
    }

    private async Task WaitForPropertyAsync(string path, string propertyName, string expected)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            using HttpResponseMessage response = await fixture.Client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                using JsonDocument document = await ReadJsonAsync(response);
                if (document.RootElement.GetProperty(propertyName).GetString() == expected)
                {
                    return;
                }
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"'{path}' did not reach {propertyName}={expected}.");
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }
}
