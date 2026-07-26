using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.DTOS
{
    public class PaymobWebhookResult
    {
        public Guid? PaymentId { get; set; }

        public string? ProviderPaymentReference { get; set; }

        public string? ProviderTransactionId { get; set; }

        public string ProviderStatus { get; set; } = null!;

        public bool Success { get; set; }

        public bool Pending { get; set; }

        public long AmountCents { get; set; }

        public string? Currency { get; set; }

        public string? FailureReason { get; set; }
    }

}
