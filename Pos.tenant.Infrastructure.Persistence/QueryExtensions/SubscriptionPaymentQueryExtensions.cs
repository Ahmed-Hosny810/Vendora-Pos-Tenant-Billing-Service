using Pos.tenant.Application.Features.SubscriptionPayments.Queries.GetAllQuery;
using Pos.tenant.Domain.Models;

namespace Pos.tenant.Infrastructure.Persistence.QueryExtensions;

public static class SubscriptionPaymentQueryExtensions
{
    public static IQueryable<SubscriptionPayment> ApplyFilters(this IQueryable<SubscriptionPayment> query,
        Guid tenantId, SubscriptionPaymentFilter? filter)
    {
        query = query.Where(payment => payment.TenantId == tenantId);
        if (filter == null)
            return query;

        if (filter.InvoiceId.HasValue)
            query = query.Where(payment => payment.InvoiceId == filter.InvoiceId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(payment => payment.Status == filter.Status.Trim());
        if (!string.IsNullOrWhiteSpace(filter.Method))
            query = query.Where(payment => payment.Method == filter.Method.Trim());
        if (!string.IsNullOrWhiteSpace(filter.Provider))
            query = query.Where(payment => payment.Provider == filter.Provider.Trim());
        if (filter.FromUtc.HasValue)
            query = query.Where(payment => payment.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtcExclusive.HasValue)
            query = query.Where(payment => payment.CreatedAt < filter.ToUtcExclusive.Value);

        return query;
    }

    public static IOrderedQueryable<SubscriptionPayment> ApplyOrdering(this IQueryable<SubscriptionPayment> query,
        SubscriptionPaymentOrderKey orderKey, bool orderDescending)
    {
        var ordered = orderKey switch
        {
            SubscriptionPaymentOrderKey.PaidAt => orderDescending
                ? query.OrderByDescending(payment => payment.PaidAt) : query.OrderBy(payment => payment.PaidAt),
            SubscriptionPaymentOrderKey.Amount => orderDescending
                ? query.OrderByDescending(payment => payment.Amount) : query.OrderBy(payment => payment.Amount),
            SubscriptionPaymentOrderKey.Status => orderDescending
                ? query.OrderByDescending(payment => payment.Status) : query.OrderBy(payment => payment.Status),
            _ => orderDescending
                ? query.OrderByDescending(payment => payment.CreatedAt) : query.OrderBy(payment => payment.CreatedAt)
        };

        return ordered.ThenBy(payment => payment.Id);
    }
}
