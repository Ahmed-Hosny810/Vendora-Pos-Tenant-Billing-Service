using Microsoft.EntityFrameworkCore;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;
using Pos.tenant.Infrastructure.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Infrastructure.Persistence.Repositories
{
    public class TenantSubscriptionRepositoryAsync : GenericRepositoryAsync<TenantSubscription, Guid>, ITenantSubscriptionRepositoryAsync
    {
        private readonly ApplicationDbContext _context;

        public TenantSubscriptionRepositoryAsync(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<TenantSubscription?> GetCurrentPlanByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.TenantSubscriptions
                .Include(ts => ts.Plan)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(ts => ts.TenantId == tenantId, cancellationToken);
        }

        public async Task<TenantSubscription?> GetSubscriptionAndPlanByIdAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
        {
            return await _context.TenantSubscriptions
                .Include(subscription => subscription.Plan)
                .SingleOrDefaultAsync(
                    subscription => subscription.Id == subscriptionId,
                    cancellationToken);
        }


        public async Task<IReadOnlyList<Guid>> GetSubscriptionIdsDueForRenewalAsync(
            DateTime renewBeforeUtc,
            int batchSize,
            CancellationToken cancellationToken)
        {
            return await _context.TenantSubscriptions
                .Where(subscription =>
                    (subscription.Status == TenantSubscriptionStatuses.Active ||
                     subscription.Status == TenantSubscriptionStatuses.Expired) &&
                    subscription.CurrentPeriodEnd.HasValue &&
                    subscription.CurrentPeriodEnd.Value <= renewBeforeUtc &&
                    !subscription.SubscriptionInvoices.Any(invoice =>
                        invoice.TenantId == subscription.TenantId &&
                        invoice.DueDate == subscription.CurrentPeriodEnd.Value))
                .OrderBy(subscription => subscription.CurrentPeriodEnd)
                .ThenBy(subscription => subscription.Id)
                .Take(batchSize)
                .Select(subscription => subscription.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>>GetSubscriptionIdsRequiringExpiryProcessingAsync(DateTime nowUtc,int batchSize,CancellationToken cancellationToken)
        {
            return await _context.TenantSubscriptions
                .Where(subscription =>
                    (
                        subscription.Status == TenantSubscriptionStatuses.Active &&
                        subscription.CurrentPeriodEnd.HasValue &&
                        subscription.CurrentPeriodEnd.Value <= nowUtc
                    )
                    ||
                    subscription.SubscriptionInvoices.Any(invoice =>
                        invoice.Status == InvoiceStatuses.Unpaid &&
                        invoice.DueDate < nowUtc))
                .OrderBy(subscription => subscription.CurrentPeriodEnd)
                .ThenBy(subscription => subscription.Id)
                .Take(batchSize)
                .Select(subscription => subscription.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<TenantSubscription?> GetForExpiryAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken)
        {
            return await _context.TenantSubscriptions
                .Include(subscription => subscription.Tenant)
                .Include(subscription => subscription.SubscriptionInvoices
                    .Where(invoice => invoice.Status == InvoiceStatuses.Unpaid))
                .SingleOrDefaultAsync(
                    subscription => subscription.Id == subscriptionId,
                    cancellationToken);
        }

        public async Task<bool> HasActiveSubscriptionAsync(
            Guid tenantId,
            Guid excludedSubscriptionId,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            return await _context.TenantSubscriptions.AnyAsync(
                subscription =>
                    subscription.TenantId == tenantId &&
                    subscription.Id != excludedSubscriptionId &&
                    subscription.Status == TenantSubscriptionStatuses.Active &&
                    subscription.CurrentPeriodStart.HasValue &&
                    subscription.CurrentPeriodStart.Value <= nowUtc &&
                    subscription.CurrentPeriodEnd.HasValue &&
                    subscription.CurrentPeriodEnd.Value > nowUtc,
                cancellationToken);
        }
    }
}
