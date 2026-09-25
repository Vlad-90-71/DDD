using FluentValidation;
using DDD.Application.Services.Dto;

namespace DDD.Application.Services.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty();

        RuleFor(x => x.Price)
            .NotNull();

        RuleFor(x => x.Price.Amount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Price.Currency)
            .NotEmpty();
    }
}