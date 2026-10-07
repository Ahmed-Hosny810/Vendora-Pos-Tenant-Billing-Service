using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Pos.tenant.Application.Features.SubscriptionPayments.Queries.GetAllQuery;
using Pos.tenant.Application.Wrappers;

namespace Pos.tenant.Application.Interfaces.Repositories
{
    public interface ISubscriptionPaymentRepositoryAsync:IGenericRepositoryAsync<SubscriptionPayment,Guid>
    {
        Task<SubscriptionPayment?> GetByTenantAndIdAsync(Guid tenantId, Guid paymentId, CancellationToken cancellationToken);

        Task<PagedResponse<IEnumerable<SubscriptionPayment>>> GetPaymentsPagedResponseAsync(
            Guid tenantId, SubscriptionPaymentFilter? filter, SubscriptionPaymentOrderKey orderKey,
            bool orderDescending, int pageNumber, int pageSize, CancellationToken cancellationToken);

        Task<SubscriptionPayment?> GetByIdempotencyKeyAsync(
            Guid tenantId,
            string idempotencyKey,
            CancellationToken cancellationToken = default);

        Task<SubscriptionPayment?> GetByIdWithInvoiceAsync(
             Guid paymentId,
             CancellationToken cancellationToken = default);

        Task<SubscriptionPayment?> GetByProviderPaymentReferenceWithInvoiceAsync(
             string providerPaymentReference,
             CancellationToken cancellationToken = default);

    }
}
