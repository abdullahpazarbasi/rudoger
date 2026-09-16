using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Inventory.Application;

namespace Rudoger.Modules.Inventory.Presentation;

[ApiController]
[Authorize]
[Route("api/v1/inventory/stock-items")]
public sealed class StockItemsController(InventoryApplicationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<StockItemView>> CreateAsync(
        CreateStockItemRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        StockItemView item = await service.CreateAsync(
            new CreateStockItemCommand(request.ProductId, request.OpeningQuantity, idempotencyKey),
            cancellationToken);
        return Created($"/api/v1/inventory/stock-items/{item.Id}", item);
    }

    [HttpGet]
    public async Task<ActionResult<Page<StockItemView>>> ListAsync(
        [FromQuery] Guid? productId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(productId, pageNumber, pageSize, cancellationToken));
    }

    [HttpGet("{stockItemId:guid}")]
    public async Task<ActionResult<StockItemView>> GetAsync(Guid stockItemId, CancellationToken cancellationToken)
    {
        return Ok(await service.GetAsync(stockItemId, cancellationToken));
    }

    [HttpPost("{stockItemId:guid}/movements")]
    public async Task<ActionResult<StockMovementView>> MoveAsync(
        Guid stockItemId,
        CreateStockMovementRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        StockMovementView movement = await service.MoveAsync(
            stockItemId,
            new CreateStockMovementCommand(request.Type, request.Quantity, idempotencyKey),
            cancellationToken);
        return Created($"/api/v1/inventory/stock-items/{stockItemId}/movements", movement);
    }

    [HttpGet("{stockItemId:guid}/movements")]
    public async Task<ActionResult<Page<StockMovementView>>> ListMovementsAsync(
        Guid stockItemId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListMovementsAsync(stockItemId, pageNumber, pageSize, cancellationToken));
    }
}
