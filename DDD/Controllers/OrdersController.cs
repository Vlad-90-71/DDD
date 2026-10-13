using DDD.Application;
using DDD.Application.Services.OrderService;
using DDD.Eventing.Contracts;
using DDD.Infrastructure.Outbox;
using Microsoft.AspNetCore.Mvc;

namespace DDD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(
    IOrderService orderService, 
    IOutboxService outboxService,
    IEventReplayService eventReplayService) : ControllerBase
{
    [HttpGet("test")]
    public async Task<ActionResult<string>> Test(CancellationToken cancellationToken)
    {

        await orderService.Test(cancellationToken);

        return Ok("Ok");
    }

    [HttpPost("debug/failure/{enabled:bool}")]
    public IActionResult SetFailureSimulation(bool enabled, [FromServices] IFailureSimulator failureSimulator)
    {
        failureSimulator.Enabled = enabled;

        return Ok(new
        {
            failureSimulation = failureSimulator.Enabled
        });
    }

    [HttpPost("debug/events/{eventId:guid}/replay")]
    public async Task<IActionResult> ReplayEvent(Guid eventId, CancellationToken cancellationToken)
    {
        var replayed = await eventReplayService.ReplayAsync(eventId, cancellationToken);

        if (!replayed)
        {
            return Conflict(new
            {
                EventId = eventId,
                Message = "Event was not found in a failed state or has already been replayed."
            });
        }

        return Accepted(new
        {
            EventId = eventId,
            Message = "Event replay has been scheduled through the Outbox."
        });
    }


    [HttpPost("debug/outbox/{id:long}/redeliver")]
    public async Task<IActionResult> Redeliver(long id, CancellationToken cancellationToken)
    {
        await outboxService.RedeliverAsync(id, cancellationToken);

        return Ok();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(int id, CancellationToken cancellationToken) =>
        await orderService.GetOrderByIdAsync(id, cancellationToken) is { } response ? Ok(response) : NotFound();

    // GET: api/Orders?statusId=2
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAllOrders(
        [FromQuery] GetOrdersQuery query, CancellationToken cancellationToken) =>
            Ok(await orderService.GetAllOrdersAsync(query, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        [FromBody] CreateOrderDto orderDto, CancellationToken cancellationToken)
    {
        var response = await orderService.CreateOrderAsync(orderDto, cancellationToken);
        return CreatedAtAction(nameof(GetOrder), new { id = response.Id }, response);
    }

    // PUT: api/Orders/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderResponse>> UpdateOrder(int id, 
        [FromBody] UpdateOrderDto orderDto, CancellationToken cancellationToken) =>
            await orderService.UpdateOrderAsync(id, orderDto, cancellationToken) is { } response
                ? Ok(response)
                : NotFound(new { Message = "Заказ не найден." });

    // DELETE: api/Orders/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id, CancellationToken cancellationToken) =>
        await orderService.DeleteOrderAsync(id, cancellationToken) 
            ? NoContent() 
            : NotFound(new { Message = "Заказ не найден." });
}