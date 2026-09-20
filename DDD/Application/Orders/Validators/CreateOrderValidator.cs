using FluentValidation;
using DDD.Domain.Enums;
using DDD.Application.Common;
using DDD.Application.Orders.Dto;

namespace DDD.Application.Orders.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Имя клиента обязательно.");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("Статус заказа обязателен.");
    }
}
