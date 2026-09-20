using Microsoft.AspNetCore.Mvc;

namespace DDD.Application.Orders;

public class GetOrdersQuery
{
    [FromQuery(Name = "statusId")] // Задаем красивое имя для URL-строки
    public int? StatusId { get; set; }
}
