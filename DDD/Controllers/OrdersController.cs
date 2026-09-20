using Microsoft.AspNetCore.Mvc;
using DDD.Application.Orders;

namespace DDD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(int id) =>
        await orderService.GetOrderByIdAsync(id) is { } response ? Ok(response) : NotFound();

    // GET: api/Orders?statusId=2
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrders([FromQuery] GetOrdersQuery query) =>
        Ok(await orderService.GetAllOrdersAsync(query));

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] OrderDto dto)
    {
        var response = await orderService.CreateOrderAsync(dto);
        return CreatedAtAction(nameof(GetOrder), new { id = response.Id }, response);
    }

    // PUT: api/Orders/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderResponseDto>> UpdateOrder(int id, [FromBody] OrderDto dto) =>
        await orderService.UpdateOrderAsync(id, dto) is { } response
            ? Ok(response)
            : NotFound(new { Message = "Заказ не найден." });

    // DELETE: api/Orders/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id) =>
        await orderService.DeleteOrderAsync(id) ? NoContent() : NotFound(new { Message = "Заказ не найден." });
}
