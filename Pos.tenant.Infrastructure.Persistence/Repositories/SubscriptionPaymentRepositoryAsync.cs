using Microsoft.EntityFrameworkCore;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Domain.Models;
using Pos.tenant.Infrastructure.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

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
