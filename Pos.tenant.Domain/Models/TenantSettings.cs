using Pos.tenant.Domain.Constants;

namespace Pos.tenant.Domain.Models
{
        public class TenantSettings
        {
            public Guid TenantId { get; set; }

            public decimal DefaultVatRate { get; set; } = 14;
            public bool PricesIncludeTax { get; set; } = false;

            public string? ReceiptFooterAr { get; set; }
            public string? ReceiptFooterEn { get; set; }

            public bool AllowReturns { get; set; } = true;
            public decimal DiscountLimitPercent { get; set; } = 20;

            public string DefaultLanguage { get; set; } = SupportedLanguages.Arabic;

             //Navigation Properties
              public Tenant Tenant { get; set; } = null!;
        }
    
}
