using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Rudoger.Modules.Authn.Presentation;
using Rudoger.Modules.Inventory.Presentation;
using Rudoger.Modules.Order.Presentation;
using Rudoger.Modules.Product.Presentation;

namespace Rudoger.Api;

/// <summary>
/// Publishes one worked example per request body so "Try it out" in Swagger UI starts from a request
/// the API accepts. Followed in order, the examples walk through token exchange, product creation, stock
/// opening and receipt, order placement and shipment. They are built from the contract records and
/// serialized with the API's own JSON options, so a contract change that breaks them fails the build
/// instead of drifting silently. <see cref="Guid.Empty"/> stands in for identifiers the caller must copy
/// from an earlier response.
/// </summary>
public sealed class RequestExampleSchemaTransformer : IOpenApiSchemaTransformer
{
    private readonly Dictionary<Type, JsonNode> _examples;

    public RequestExampleSchemaTransformer(IOptions<JsonOptions> jsonOptions)
    {
        var serializerOptions = new JsonSerializerOptions(jsonOptions.Value.JsonSerializerOptions)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        _examples = new Dictionary<Type, JsonNode>
        {
            [typeof(TokenExchangeRequest)] = ToNode(
                new TokenExchangeRequest("abdullah", "12345678"),
                serializerOptions),
            [typeof(CreateProductRequest)] = ToNode(
                new CreateProductRequest(
                    "COFFEE-001",
                    "Coffee",
                    "EA",
                    2.50m,
                    "TRY",
                    [
                        new ProductPackagingRequest(null, 0, "EA", 1m, "869000000001", null, null, null, null),
                        new ProductPackagingRequest(null, 1, "CASE", 10m, "869000000010", null, null, null, null),
                    ]),
                serializerOptions),
            [typeof(CreateStockItemRequest)] = ToNode(
                new CreateStockItemRequest(Guid.Empty, "CASE", 10m),
                serializerOptions),
            [typeof(CreateStockMovementRequest)] = ToNode(
                new CreateStockMovementRequest(StockMovementTypeContract.Receipt, "CASE", 2m),
                serializerOptions),
            [typeof(CreateOrderRequest)] = ToNode(
                new CreateOrderRequest([new OrderPlacementLineRequest(Guid.Empty, "CASE", 2m)]),
                serializerOptions),
            [typeof(CreateOrderTransitionRequest)] = ToNode(
                new CreateOrderTransitionRequest(OrderTransitionTargetContract.Shipped),
                serializerOptions),
        };
    }

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Property schemas share the declaring type's JsonTypeInfo; only the component schema gets the example.
        if (context.JsonPropertyInfo is null && _examples.TryGetValue(context.JsonTypeInfo.Type, out JsonNode? example))
        {
            schema.Examples = [example.DeepClone()];
        }

        return Task.CompletedTask;
    }

    private static JsonNode ToNode<TRequest>(TRequest request, JsonSerializerOptions options)
    {
        return JsonSerializer.SerializeToNode(request, options)
            ?? throw new InvalidOperationException($"'{typeof(TRequest).Name}' produced no example.");
    }
}
