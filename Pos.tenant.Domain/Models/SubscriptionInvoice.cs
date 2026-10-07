using Pos.tenant.Domain.Common;
using Pos.tenant.Domain.Constants;

namespace Pos.tenant.Domain.Models
{
    public partial class SubscriptionInvoice : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid TenantSubscriptionId { get; set; }

        public string InvoiceNumber { get; set; } = null!;

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public decimal Total { get; set; }

        public string Status { get; set; } = InvoiceStatuses.Unpaid;

        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        //Navigation Properties
        public Tenant Tenant { get; set; } = null!;
        public TenantSubscription TenantSubscription { get; set; } = null!;

        public ICollection<SubscriptionPayment> SubscriptionPayments { get; set; } = new HashSet<SubscriptionPayment>();

        public void MarkPaid(DateTime paidAt)
        {
            Status = InvoiceStatuses.Paid;
            PaidAt = paidAt;
        }

        public void MarkOverdue()
        {
            if (Status != InvoiceStatuses.Paid)
                Status = InvoiceStatuses.Overdue;
        }

        public void Cancel()
        {
            Status = InvoiceStatuses.Cancelled;
        }
    }
}
