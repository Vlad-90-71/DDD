using Microsoft.AspNetCore.Mvc;
using DDD.Application.Orders;

namespace DDD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(int id) =>
        await orderService.GetOrderByIdAsync(id) is { } response ? Ok(response) : NotFound();

    // GET: api/Orders?statusId=2
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAllOrders([FromQuery] GetOrdersQuery query) =>
        Ok(await orderService.GetAllOrdersAsync(query));

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] OrderDto orderDto)
    {
        var response = await orderService.CreateOrderAsync(orderDto);
        return CreatedAtAction(nameof(GetOrder), new { id = response.Id }, response);
    }

    // PUT: api/Orders/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderResponse>> UpdateOrder(int id, [FromBody] OrderDto orderDto) =>
        await orderService.UpdateOrderAsync(id, orderDto) is { } response
            ? Ok(response)
            : NotFound(new { Message = "Заказ не найден." });

    // DELETE: api/Orders/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id) =>
        await orderService.DeleteOrderAsync(id) ? NoContent() : NotFound(new { Message = "Заказ не найден." });
}
