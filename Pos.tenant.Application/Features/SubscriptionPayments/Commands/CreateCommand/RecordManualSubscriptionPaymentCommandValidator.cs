using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.SubscriptionPayments.Commands.CreateCommand
{
    public class RecordManualSubscriptionPaymentCommandValidator: AbstractValidator<RecordManualSubscriptionPaymentCommand>
    {
        public RecordManualSubscriptionPaymentCommandValidator()
        {
            RuleFor(x => x.InvoiceId)
                .NotEmpty();

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Payment amount must be greater than 0.");

            RuleFor(x => x.Method)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.ReferenceNumber)
                .MaximumLength(200);
        }
    }
}
