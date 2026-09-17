using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rudoger.BuildingBlocks.Application;
using Rudoger.BuildingBlocks.Presentation;
using Rudoger.Modules.Order.Application;

namespace Rudoger.Modules.Order.Presentation;

[ApiController]
[Authorize]
[Route("api/v1/order/orders")]
public sealed class OrdersController(OrderApplicationService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OrderPlacementResponse>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<OrderPlacementResponse>> CreateAsync(
        CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        OrderPlacementView placement = await service.CreatePlacementAsync(
            new CreateOrderPlacementCommand(
                idempotencyKey,
                request.Lines.Select(line => new OrderPlacementLineInput(
                    line.ProductId,
                    line.UomCode,
                    line.Quantity)).ToArray()),
            cancellationToken);
        return Accepted(
            $"/api/v1/order/order-placements/{placement.Id}",
            OrderPlacementResponse.From(placement));
    }

    [HttpGet]
    public async Task<ActionResult<PageResponse<OrderResponse>>> ListAsync(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        Page<OrderView> page = await service.ListAsync(pageNumber, pageSize, cancellationToken);
        return Ok(PageResponseFactory.From(page, OrderResponse.From));
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderResponse>> GetAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return Ok(OrderResponse.From(await service.GetAsync(orderId, cancellationToken)));
    }

    [HttpGet("/api/v1/order/order-placements/{placementId:guid}")]
    public async Task<ActionResult<OrderPlacementResponse>> GetPlacementAsync(
        Guid placementId,
        CancellationToken cancellationToken)
    {
        return Ok(OrderPlacementResponse.From(await service.GetPlacementAsync(placementId, cancellationToken)));
    }

    [HttpPost("{orderId:guid}/transitions")]
    [ProducesResponseType<OrderTransitionResponse>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<OrderTransitionResponse>> TransitionAsync(
        Guid orderId,
        CreateOrderTransitionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        OrderTransitionView transition = await service.RequestTransitionAsync(
            orderId,
            request.ToTarget(),
            cancellationToken);
        return Accepted(
            $"/api/v1/order/orders/{orderId}/transitions/{transition.Id}",
            OrderTransitionResponse.From(transition));
    }

    [HttpGet("{orderId:guid}/transitions/{transitionId:guid}")]
    public async Task<ActionResult<OrderTransitionResponse>> GetTransitionAsync(
        Guid orderId,
        Guid transitionId,
        CancellationToken cancellationToken)
    {
        return Ok(OrderTransitionResponse.From(
            await service.GetTransitionAsync(orderId, transitionId, cancellationToken)));
    }
}
