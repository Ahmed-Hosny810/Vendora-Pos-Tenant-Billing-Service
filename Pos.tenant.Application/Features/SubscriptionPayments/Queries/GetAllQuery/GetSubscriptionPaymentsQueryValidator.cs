using FluentValidation;
using Pos.tenant.Domain.Constants;

namespace Pos.tenant.Application.Features.SubscriptionPayments.Queries.GetAllQuery;

public class GetSubscriptionPaymentsQueryValidator : AbstractValidator<GetSubscriptionPaymentsQuery>
{
    public GetSubscriptionPaymentsQueryValidator()
    {
        RuleFor(query => query.Parameter).NotNull();
        When(query => query.Parameter != null, () =>
        {
            RuleFor(query => query.Parameter.OrderKey).IsInEnum();
            When(query => query.Parameter.Filter != null, () =>
            {
                RuleFor(query => query.Parameter.Filter!.InvoiceId)
                    .Must(id => !id.HasValue || id.Value != Guid.Empty);
                RuleFor(query => query.Parameter.Filter!.Status)
                    .Must(status => string.IsNullOrWhiteSpace(status) ||
                        new[] { PaymentStatuses.Pending, PaymentStatuses.Completed,
                            PaymentStatuses.Failed, PaymentStatuses.Refunded }.Contains(status.Trim()))
                    .WithMessage("Invalid payment status.");
                RuleFor(query => query.Parameter.Filter!.Method).MaximumLength(50);
                RuleFor(query => query.Parameter.Filter!.Provider).MaximumLength(50);
                RuleFor(query => query.Parameter.Filter!.FromUtc)
                    .Must(date => !date.HasValue || date.Value.Kind == DateTimeKind.Utc)
                    .WithMessage("FromUtc must be a UTC date.");
                RuleFor(query => query.Parameter.Filter!.ToUtcExclusive)
                    .Must(date => !date.HasValue || date.Value.Kind == DateTimeKind.Utc)
                    .WithMessage("ToUtcExclusive must be a UTC date.");
                RuleFor(query => query.Parameter.Filter)
                    .Must(filter => !filter!.FromUtc.HasValue || !filter.ToUtcExclusive.HasValue ||
                        filter.FromUtc.Value < filter.ToUtcExclusive.Value)
                    .WithMessage("FromUtc must precede ToUtcExclusive.");
            });
        });
    }
}
