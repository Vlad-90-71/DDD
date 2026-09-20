using FluentValidation;
using DDD.Domain.Enums;
using DDD.Application.Common;

namespace DDD.Application.Orders.Validators;

public class UpdateOrderValidator : AbstractValidator<UpdateOrderDto>
{
    public UpdateOrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Имя клиента обязательно при обновлении.");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("Статус заказа обязателен.");
    }
}
