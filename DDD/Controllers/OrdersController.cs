using Microsoft.AspNetCore.Mvc;
using DDD.Domain.Enums;
using DDD.Domain.Entities;

namespace DDD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    // Эндпоинт для получения заказа (проверим, как статус уходит в JSON)
    [HttpGet("{id:int}")]
    public ActionResult<Order> GetOrder(int id)
    {
        var order = new Order
        {
            Id = id,
            CustomerName = "Тестовый Клиент",
            Status = OrderStatus.Processing // Будет автоматически сериализован в число 2
        };

        return Ok(order);
    }

    // Эндпоинт для создания заказа (проверим, как принимает int)
    [HttpPost]
    public IActionResult CreateOrder([FromBody] Order order)
    {
        // Здесь сработает наш JsonConverter и превратит пришедший int в объект OrderStatus
        return Ok(new { Message = $"Заказ создан со статусом: {order.Status}" });
    }
}
