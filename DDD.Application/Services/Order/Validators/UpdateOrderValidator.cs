using FluentValidation;
using DDD.Application.Services.Dto;

namespace DDD.Application.Services.Validators;

public class UpdateOrderValidator : AbstractValidator<UpdateOrderDto>
{
    public UpdateOrderValidator()
    {
        When(x => x.CustomerName is not null, () =>
        {
            RuleFor(x => x.CustomerName)
                .NotEmpty()
                .MaximumLength(200);
        });

        When(x => x.Email is not null, () =>
        {
            RuleFor(x => x.Email)
                .NotEmpty();
        });

        When(x => x.Price is not null, () =>
        {
            RuleFor(x => x.Price!.Amount)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Price!.Currency)
                .NotEmpty();
        });
    }
}