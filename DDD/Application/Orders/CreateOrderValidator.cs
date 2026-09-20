using FluentValidation;
using DDD.Domain.Enums;
using DDD.Application.Common;

namespace DDD.Application.Orders;

public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Имя клиента обязательно.");

        // Автоматическая валидация переданного int по нашему списку OrderStatus
        RuleFor(x => x.StatusId)
            .IsInSmartEnum<CreateOrderDto, OrderStatus>();
    }
}
