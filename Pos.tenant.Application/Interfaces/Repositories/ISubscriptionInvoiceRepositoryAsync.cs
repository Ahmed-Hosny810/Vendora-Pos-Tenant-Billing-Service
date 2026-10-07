using Pos.tenant.Application.Features.SubscriptionInvoices.Queries.GetAllQuery;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Models;

namespace Pos.tenant.Application.Interfaces.Repositories
{
    public interface ISubscriptionInvoiceRepositoryAsync: IGenericRepositoryAsync<SubscriptionInvoice, Guid>
    {
        Task<bool> IsInvoiceNumberExistsAsync(Guid tenantId, string invoiceNumber);
        Task<SubscriptionInvoice?> GetByTenantAndIdAsync(Guid tenantId, Guid invoiceId, CancellationToken cancellationToken);

        Task<PagedResponse<IEnumerable<SubscriptionInvoice>>> GetInvoicesPagedResponseAsync(Guid tenantId,SubscriptionInvoiceFilter filter,
            SubscriptionInvoiceOrderKey orderKey, bool orderDescending, int currentPage, int pageSize);

        /// <summary>
        /// Gets an invoice by tenant, subscription, and exact due date.
        /// </summary>
        /// <param name="tenantId">The tenant that owns the subscription.</param>
        /// <param name="subscriptionId">The subscription being renewed.</param>
        /// <param name="dueDate">The subscription's saved CurrentPeriodEnd used when creating the renewal invoice.</param>
        /// <param name="cancellationToken">Token used to cancel the database query.</param>
        /// <returns>The matching invoice, or null if none exists.</returns>
        Task<SubscriptionInvoice?> GetBySubscriptionAndDueDateAsync(Guid tenantId,Guid subscriptionId,DateTime dueDate,CancellationToken cancellationToken);

    }
}
