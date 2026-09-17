using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Presentation;
using Rudoger.Modules.Inventory.Application;

namespace Rudoger.Modules.Inventory.Presentation;

[ApiController]
[Authorize]
[Route("api/v1/inventory/stock-items")]
public sealed class StockItemsController(InventoryApplicationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<StockItemResponse>> CreateAsync(
        CreateStockItemRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        StockItemView item = await service.CreateAsync(
            new CreateStockItemCommand(request.ProductId, request.UomCode, request.OpeningQuantity, idempotencyKey),
            cancellationToken);
        return Created($"/api/v1/inventory/stock-items/{item.Id}", StockItemResponse.From(item));
    }

    [HttpGet]
    public async Task<ActionResult<PageResponse<StockItemResponse>>> ListAsync(
        [FromQuery] Guid? productId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        Page<StockItemView> page = await service.ListAsync(productId, pageNumber, pageSize, cancellationToken);
        return Ok(PageResponseFactory.From(page, StockItemResponse.From));
    }

    [HttpGet("{stockItemId:guid}")]
    public async Task<ActionResult<StockItemResponse>> GetAsync(Guid stockItemId, CancellationToken cancellationToken)
    {
        return Ok(StockItemResponse.From(await service.GetAsync(stockItemId, cancellationToken)));
    }

    [HttpPost("{stockItemId:guid}/movements")]
    public async Task<ActionResult<StockMovementResponse>> MoveAsync(
        Guid stockItemId,
        CreateStockMovementRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        StockMovementView movement = await service.MoveAsync(
            stockItemId,
            new CreateStockMovementCommand(
                request.ToMovementType(),
                request.UomCode,
                request.Quantity,
                idempotencyKey),
            cancellationToken);
        return Created(
            $"/api/v1/inventory/stock-items/{stockItemId}/movements",
            StockMovementResponse.From(movement));
    }

    [HttpGet("{stockItemId:guid}/movements")]
    public async Task<ActionResult<PageResponse<StockMovementResponse>>> ListMovementsAsync(
        Guid stockItemId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        Page<StockMovementView> page = await service.ListMovementsAsync(
            stockItemId,
            pageNumber,
            pageSize,
            cancellationToken);
        return Ok(PageResponseFactory.From(page, StockMovementResponse.From));
    }
}
