using DDD.Application.Orders;
using DDD.Domain.Common.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace DDD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet("test")]
    public ActionResult<string> Test()
    {
        var a = new Money(100, "usd");
        var b = new Money(100, "USD");
        var c = new Money(200, "USD");

        List<bool> xx = [a == b, a == c, a.Equals(b)];


        return Ok(string.Join(", ", xx));
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
