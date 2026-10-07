using Pos.tenant.Domain.Common;
using Pos.tenant.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Domain.Models
{
    public partial class Tenant:BaseEntity
    {
        public string? NameAr { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public string NameEn { get; set; }= null!;
        public string BusinessTypeCode { get; set; } = null!;

        public string Status { get; set; } = TenantStatuses.Pending;

        public string CurrencyCode { get; set; } = "EGP";
        public string InventoryMode { get; set; } = TenantInventoryModes.TrackStock;

        //Navigation Properties
        public TenantSettings? TenantSettings { get; set; }

        public TenantUsageCounters? TenantUsageCounters { get; set; }

        public ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new HashSet<TenantSubscription>();
        public ICollection<TenantStatusHistory> TenantStatusHistory { get; set; } = new HashSet<TenantStatusHistory>();

        public ICollection<SubscriptionInvoice> SubscriptionInvoices { get; set; } = new HashSet<SubscriptionInvoice>();

        public ICollection<SubscriptionPayment> SubscriptionPayments { get; set; } = new HashSet<SubscriptionPayment>();


        public string GetDisplayName(string? language)
        {
            var isArabic = string.Equals(language, "ar", StringComparison.OrdinalIgnoreCase);

            return isArabic
                ? NameAr ?? NameEn ?? string.Empty
                : NameEn ?? NameAr ?? string.Empty;
        }

        public void Activate()
        {
            Status = TenantStatuses.Active;
        }

        public void Suspend()
        {
            Status = TenantStatuses.Suspended;
        }

        public void Cancel()
        {
            Status = TenantStatuses.Cancelled;
        }
    }
}
