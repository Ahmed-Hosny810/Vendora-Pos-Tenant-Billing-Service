using FluentValidation;

namespace Pos.tenant.Application.Features.SubscriptionPayments.Queries.GetByIdQuery;

public class GetSubscriptionPaymentByIdQueryValidator : AbstractValidator<GetSubscriptionPaymentByIdQuery>
{
    public GetSubscriptionPaymentByIdQueryValidator()
    {
        RuleFor(query => query.PaymentId).NotEmpty();
    }
}
