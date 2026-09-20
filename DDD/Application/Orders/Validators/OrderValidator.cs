using FluentValidation;

namespace DDD.Application.Orders.Validators;

public class OrderValidator : AbstractValidator<OrderDto>
{
    public OrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Имя клиента обязательно.");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("Статус заказа обязателен.");
    }
}
