using Pos.tenant.Application.Parameters;

namespace Pos.tenant.Application.Features.SubscriptionPayments.Queries.GetAllQuery;

public class GetSubscriptionPaymentsQueryParameter : RequestParameter<SubscriptionPaymentOrderKey>
{
    public SubscriptionPaymentFilter? Filter { get; set; }
}

public class SubscriptionPaymentFilter
{
    public Guid? InvoiceId { get; set; }
    public string? Status { get; set; }
    public string? Method { get; set; }
    public string? Provider { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtcExclusive { get; set; }
}

public enum SubscriptionPaymentOrderKey
{
    CreatedAt,
    PaidAt,
    Amount,
    Status
}
