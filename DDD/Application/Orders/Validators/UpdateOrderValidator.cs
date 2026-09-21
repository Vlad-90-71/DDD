using FluentValidation;

namespace DDD.Application.Orders.Validators;

public class UpdateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public UpdateOrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Имя клиента не может быть пустой строкой.")
            .MaximumLength(100).WithMessage("Имя клиента не может быть длиннее 100 символов.")
            .When(x => x.CustomerName != null);
    }
}
