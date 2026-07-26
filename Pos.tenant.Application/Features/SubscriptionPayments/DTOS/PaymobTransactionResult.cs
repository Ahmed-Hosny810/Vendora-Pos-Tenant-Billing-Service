using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.SubscriptionPayments.DTOS
{
    public class PaymobTransactionResult
    {
        public string ProviderTransactionId { get; set; } = null!;

        public string? ProviderPaymentReference { get; set; }

        public string ProviderStatus { get; set; } = null!;

        public bool Success { get; set; }

        public bool Pending { get; set; }

        public bool ErrorOccured { get; set; }

        public long AmountCents { get; set; }

        public string? Currency { get; set; }

        public long IntegrationId { get; set; }

        public bool IsRefunded { get; set; }

        public long RefundedAmountCents { get; set; }

        public bool IsVoided { get; set; }

        public bool IsCaptured { get; set; }

        public string? FailureReason { get; set; }

        public string? SourceType { get; set; }

        public string? SourceSubType { get; set; }

        public string? SourcePan { get; set; }
    }
}
