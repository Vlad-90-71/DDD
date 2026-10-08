using FluentValidation;
using DDD.Application.Common;
using DDD.Domain.Entities.Order;

namespace DDD.Application.Services.OrderService.Validators;

public class GetOrdersQueryValidator : AbstractValidator<GetOrdersQuery>
{
    public GetOrdersQueryValidator()
    {
        // Проверяем статус ТОЛЬКО если клиент передал его в URL
        RuleFor(x => x.StatusId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .DependentRules(() =>
            {
                // Приводим int? к int через .Value и вызываем наше расширение
                RuleFor(x => x.StatusId!.Value)
                    .IsInSmartEnum<GetOrdersQuery, OrderStatus>();
            })
            .When(x => x.StatusId.HasValue);
    }
}
