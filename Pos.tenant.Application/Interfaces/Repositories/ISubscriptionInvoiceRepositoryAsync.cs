using Pos.tenant.Application.Features.SubscriptionInvoices.Queries.GetAllQuery;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Interfaces.Repositories
{
    public interface ISubscriptionInvoiceRepositoryAsync: IGenericRepositoryAsync<SubscriptionInvoice, Guid>
    {
        Task<bool> IsInvoiceNumberExistsAsync(Guid tenantId, string invoiceNumber);
        Task<SubscriptionInvoice?> GetByTenantAndIdAsync(Guid tenantId, Guid invoiceId, CancellationToken cancellationToken);

        Task<PagedResponse<IEnumerable<SubscriptionInvoice>>> GetInvoicesPagedResponseAsync(Guid tenantId,SubscriptionInvoiceFilter filter,
            SubscriptionInvoiceOrderKey orderKey, bool orderDescending, int currentPage, int pageSize);
    }
}
