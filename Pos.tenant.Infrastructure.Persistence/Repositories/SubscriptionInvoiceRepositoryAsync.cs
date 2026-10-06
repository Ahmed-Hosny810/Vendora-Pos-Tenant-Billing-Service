using Microsoft.EntityFrameworkCore;
using Pos.tenant.Application.Features.SubscriptionInvoices.Queries.GetAllQuery;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Models;
using Pos.tenant.Infrastructure.Persistence.Contexts;
using Pos.tenant.Infrastructure.Persistence.QueryExtensions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection.Metadata;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Pos.tenant.Infrastructure.Persistence.Repositories
{
    public class SubscriptionInvoiceRepositoryAsync : GenericRepositoryAsync<SubscriptionInvoice, Guid>, ISubscriptionInvoiceRepositoryAsync
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionInvoiceRepositoryAsync(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<bool> IsInvoiceNumberExistsAsync(Guid tenantId, string invoiceNumber)
        {
            return _context.SubscriptionInvoices
                .AnyAsync(i => i.TenantId == tenantId && i.InvoiceNumber == invoiceNumber);
        }
        public Task<SubscriptionInvoice?> GetByTenantAndIdAsync(Guid tenantId, Guid invoiceId, CancellationToken cancellationToken)
        {
            return _context.SubscriptionInvoices.SingleOrDefaultAsync(
                invoice => invoice.TenantId == tenantId && invoice.Id == invoiceId, cancellationToken);
        }
        public async Task<PagedResponse<IEnumerable<SubscriptionInvoice>>> GetInvoicesPagedResponseAsync(Guid tenantId, SubscriptionInvoiceFilter filter, 
            SubscriptionInvoiceOrderKey orderKey, bool orderDescending, int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            

            var query= _context.SubscriptionInvoices.AsNoTracking().Where(i => i.TenantId == tenantId);

            var totalRecords= await query.ApplyFilters(filter).CountAsync();

            var invoices = await query
                             .ApplyFilters(filter)
                             .ApplyOrdering(orderKey, orderDescending)
                             .Skip((pageNumber - 1) * pageSize)
                             .Take(pageSize)
                             .ToListAsync();

            return new PagedResponse<IEnumerable<SubscriptionInvoice>>(invoices,pageNumber, pageSize, totalRecords);   
        }

    }
}
