using FluentValidation;
using Pos.tenant.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.Tenants.Commands.Settings
{
    public class UpdateTenantSettingsCommandValidator
        : AbstractValidator<UpdateTenantSettingsCommand>
    {
        public UpdateTenantSettingsCommandValidator()
        {
            RuleFor(x => x.DefaultVatRate)
                .GreaterThanOrEqualTo(0)
                .WithMessage("VAT rate must be greater than or equal to 0.");

            RuleFor(x => x.DiscountLimitPercent)
                .InclusiveBetween(0, 100)
                .WithMessage("Discount limit percent must be between 0 and 100.");

            RuleFor(x => x.DefaultLanguage)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(language =>
                {
                    var normalizedLanguage = language.Trim().ToLowerInvariant();

                    return normalizedLanguage == SupportedLanguages.Arabic
                        || normalizedLanguage == SupportedLanguages.English;
                })
                .WithMessage("Default language must be either 'ar' or 'en'.");

            RuleFor(x => x.ReceiptFooterAr)
                .MaximumLength(500);

            RuleFor(x => x.ReceiptFooterEn)
                .MaximumLength(500);
        }
    }
}
