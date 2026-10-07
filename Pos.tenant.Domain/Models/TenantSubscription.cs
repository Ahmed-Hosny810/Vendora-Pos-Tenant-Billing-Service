using Pos.tenant.Domain.Common;
using Pos.tenant.Domain.Constants;


namespace Pos.tenant.Domain.Models
{
    public partial class TenantSubscription : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid PlanId { get; set; }

        public string Status { get; set; } = TenantSubscriptionStatuses.Pending;

        public DateTime? CurrentPeriodStart { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }
        public DateTime? GracePeriodEndsAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Tenant Tenant { get; set; } = null!;
        public SubscriptionPlan Plan { get; set; } = null!;

        public ICollection<SubscriptionInvoice> SubscriptionInvoices { get; set; } = new HashSet<SubscriptionInvoice>();

        public void ChangePlan(Guid newPlanId)
        {
            PlanId = newPlanId;
        }

        public void MarkActive(DateTime periodStart, DateTime periodEnd)
        {
            Status = TenantSubscriptionStatuses.Active;
            CurrentPeriodStart = periodStart;
            CurrentPeriodEnd = periodEnd;
            GracePeriodEndsAt = null;
        }

        public void MarkPastDue(DateTime gracePeriodEndsAt)
        {
            Status = TenantSubscriptionStatuses.PastDue;
            GracePeriodEndsAt = gracePeriodEndsAt;
        }

        public void Expire()
        {
            Status = TenantSubscriptionStatuses.Expired;
        }

        public void Cancel()
        {
            Status = TenantSubscriptionStatuses.Cancelled;
        }
    }
}
