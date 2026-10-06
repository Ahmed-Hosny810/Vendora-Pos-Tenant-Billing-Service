using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Pos.tenant.Infrastructure.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Infrastructure.Persistence.Repositories
{
    public class TenantUsageCountersRepositoryAsync : GenericRepositoryAsync<TenantUsageCounters, Guid>, ITenantUsageCountersRepositoryAsync
    {
        private readonly ApplicationDbContext _context;

        public TenantUsageCountersRepositoryAsync(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<TenantUsageCounters?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            return _context.TenantUsageCounters.SingleOrDefaultAsync(
                counters => counters.TenantId == tenantId, cancellationToken);
        }
    }
}
