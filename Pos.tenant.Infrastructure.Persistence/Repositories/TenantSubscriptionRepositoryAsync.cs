using Microsoft.EntityFrameworkCore;
using Pos.tenant.Application.Interfaces.Repositories;
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
    }
}
