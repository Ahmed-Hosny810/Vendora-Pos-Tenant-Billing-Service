using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.SubscriptionInvoices.DTOS
{
    public class SubscriptionInvoiceDto
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }
        public Guid TenantSubscriptionId { get; set; }

        public string InvoiceNumber { get; set; } = null!;

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public decimal Total { get; set; }

        public string Status { get; set; } = null!;

        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
