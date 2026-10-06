using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Interfaces.Repositories
{
    public interface ISubscriptionPaymentRepositoryAsync:IGenericRepositoryAsync<SubscriptionPayment,Guid>
    {
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
