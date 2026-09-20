using Microsoft.AspNetCore.Mvc;
using DDD.Application.Orders;
using DDD.Application.Orders.Dto;

namespace DDD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(int id)
    {
        var response = await orderService.GetOrderByIdAsync(id);
        return response is null ? NotFound() : Ok(response);
    }

    // GET: api/Orders?statusId=2
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrders([FromQuery] GetOrdersQuery query)
    {
        var response = await orderService.GetAllOrdersAsync(query);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var response = await orderService.CreateOrderAsync(dto);
        return CreatedAtAction(nameof(GetOrder), new { id = response.Id }, response);
    }

    // PUT: api/Orders/5 (Обновление)
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderResponseDto>> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        var response = await orderService.UpdateOrderAsync(id, dto);
        return response is null ? NotFound(new { Message = "Заказ не найден." }) : Ok(response);
    }

    // DELETE: api/Orders/5 (Удаление)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var success = await orderService.DeleteOrderAsync(id);
        return success ? NoContent() : NotFound(new { Message = "Заказ не найден." });
    }
}
