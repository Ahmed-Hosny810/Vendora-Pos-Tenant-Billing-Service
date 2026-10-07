using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.SubscriptionPayments.DTOS
{
    public class SubscriptionPaymentDto
    {
        public Guid PaymentId { get; set; }

        public Guid TenantId { get; set; }

        public Guid InvoiceId { get; set; }

        public decimal Amount { get; set; }

        public string Method { get; set; } = null!;

        public string? ReferenceNumber { get; set; }

        public string? Provider { get; set; }

        public string? ProviderPaymentReference { get; set; }

        public string? ProviderTransactionId { get; set; }

        public string? ProviderStatus { get; set; }

        public string? FailureReason { get; set; }

        public string Status { get; set; } = null!;

        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
