using Microsoft.EntityFrameworkCore;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Domain.Models;
using Pos.tenant.Infrastructure.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Text;
using Pos.tenant.Application.Features.SubscriptionPayments.Queries.GetAllQuery;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Infrastructure.Persistence.QueryExtensions;

namespace Pos.tenant.Infrastructure.Persistence.Repositories
{
    public class SubscriptionPaymentRepositoryAsync : GenericRepositoryAsync<SubscriptionPayment, Guid>, ISubscriptionPaymentRepositoryAsync
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionPaymentRepositoryAsync(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SubscriptionPayment?> GetByIdempotencyKeyAsync(
           Guid tenantId,
           string idempotencyKey,
           CancellationToken cancellationToken = default)
        {
            return await _context.SubscriptionPayments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IdempotencyKey == idempotencyKey,cancellationToken);
        }

        public Task<SubscriptionPayment?> GetByTenantAndIdAsync(Guid tenantId, Guid paymentId, CancellationToken cancellationToken)
        {
            return _context.SubscriptionPayments.SingleOrDefaultAsync(
                payment => payment.TenantId == tenantId && payment.Id == paymentId, cancellationToken);
        }

        public async Task<PagedResponse<IEnumerable<SubscriptionPayment>>> GetPaymentsPagedResponseAsync(
            Guid tenantId, SubscriptionPaymentFilter? filter, SubscriptionPaymentOrderKey orderKey,
            bool orderDescending, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _context.SubscriptionPayments.AsNoTracking().ApplyFilters(tenantId, filter);
            var totalCount = await query.CountAsync(cancellationToken);
            var offset = ((long)pageNumber - 1) * pageSize;

            var payments = offset >= totalCount
                ? new List<SubscriptionPayment>()
                : await query.ApplyOrdering(orderKey, orderDescending)
                    .Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);

            return new PagedResponse<IEnumerable<SubscriptionPayment>>(payments, pageNumber, pageSize, totalCount);
        }

        public async Task<SubscriptionPayment?> GetByIdWithInvoiceAsync(Guid paymentId, CancellationToken cancellationToken = default)
        {
            return await _context.SubscriptionPayments
                     .Include(x => x.Invoice)
                     .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
        }

        public async Task<SubscriptionPayment?> GetByProviderPaymentReferenceWithInvoiceAsync(string providerPaymentReference, CancellationToken cancellationToken = default)
        {
            return await _context.SubscriptionPayments
                .Include(x => x.Invoice)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProviderPaymentReference == providerPaymentReference, cancellationToken);
        }
    }
}
