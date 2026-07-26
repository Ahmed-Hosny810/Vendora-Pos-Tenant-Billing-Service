using Pos.tenant.Domain.Common;
using Pos.tenant.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Domain.Models
{

    public partial class SubscriptionPayment : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid InvoiceId { get; set; }

        public decimal Amount { get; set; }

        public string Method { get; set; } = null!;
        public string? ReferenceNumber { get; set; }

        public string? Provider { get; set; }
        public string? ProviderPaymentReference { get; set; }
        public string? ProviderTransactionId { get; set; }
        public string? ProviderStatus { get; set; }
        public string? ProviderClientSecret { get; set; }
        public string? FailureReason { get; set; }

        public string? IdempotencyKey { get; set; }

        public string Status { get; set; } = PaymentStatuses.Pending;

        public DateTime? PaidAt { get; set; }

        public Tenant Tenant { get; set; } = null!;
        public SubscriptionInvoice Invoice { get; set; } = null!;

        public void MarkCompleted(DateTime paidAt, string? providerTransactionId = null)
        {
            Status = PaymentStatuses.Completed;
            PaidAt = paidAt;
            FailureReason = null;

            if (!string.IsNullOrWhiteSpace(providerTransactionId))
                ProviderTransactionId = providerTransactionId;
        }

        public void MarkPending(string? providerTransactionId = null,string? providerStatus = null)
        {
            Status = PaymentStatuses.Pending;
            PaidAt = null;
            FailureReason = null;

            if (!string.IsNullOrWhiteSpace(providerTransactionId))
                ProviderTransactionId = providerTransactionId;

            if (!string.IsNullOrWhiteSpace(providerStatus))
                ProviderStatus = providerStatus;
        }


        public void MarkFailed(string? failureReason = null, string? providerTransactionId = null)
        {
            Status = PaymentStatuses.Failed;
            PaidAt = null;
            FailureReason = failureReason;

            if (!string.IsNullOrWhiteSpace(providerTransactionId))
                ProviderTransactionId = providerTransactionId;
        }

        public void Refund()
        {
            Status = PaymentStatuses.Refunded;
        }
    }
}
