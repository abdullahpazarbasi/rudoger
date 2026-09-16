using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Presentation;

[ApiController]
[Authorize]
[Route("api/v1/order/orders")]
public sealed class OrdersController(OrderApplicationService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OrderPlacementView>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<OrderPlacementView>> CreateAsync(
        CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        OrderPlacementView placement = await service.CreatePlacementAsync(
            new CreateOrderPlacementCommand(
                idempotencyKey,
                request.Lines.Select(line => new OrderPlacementLineInput(
                    line.ProductId,
                    line.UomCode,
                    line.Quantity)).ToArray()),
            cancellationToken);
        return Accepted($"/api/v1/order/order-placements/{placement.Id}", placement);
    }

    [HttpGet]
    public async Task<ActionResult<Page<OrderView>>> ListAsync(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(pageNumber, pageSize, cancellationToken));
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderView>> GetAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return Ok(await service.GetAsync(orderId, cancellationToken));
    }

    [HttpGet("/api/v1/order/order-placements/{placementId:guid}")]
    public async Task<ActionResult<OrderPlacementView>> GetPlacementAsync(
        Guid placementId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetPlacementAsync(placementId, cancellationToken));
    }

    [HttpPost("{orderId:guid}/transitions")]
    [ProducesResponseType<OrderTransitionView>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<OrderTransitionView>> TransitionAsync(
        Guid orderId,
        CreateOrderTransitionRequest request,
        CancellationToken cancellationToken)
    {
        OrderTransitionView transition = await service.RequestTransitionAsync(orderId, request.Target, cancellationToken);
        return Accepted($"/api/v1/order/orders/{orderId}/transitions/{transition.Id}", transition);
    }

    [HttpGet("{orderId:guid}/transitions/{transitionId:guid}")]
    public async Task<ActionResult<OrderTransitionView>> GetTransitionAsync(
        Guid orderId,
        Guid transitionId,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetTransitionAsync(orderId, transitionId, cancellationToken));
    }
}
