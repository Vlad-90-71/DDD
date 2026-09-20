using FluentValidation;
using DDD.Application.Orders;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Регистрируем наш сервис
        services.AddScoped<IOrderService, OrderService>();

        // Автоматически находит и регистрирует ВСЕ валидаторы FluentValidation в этой сборке
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
