using Microsoft.EntityFrameworkCore;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Domain.Models;
using Pos.tenant.Infrastructure.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Infrastructure.Persistence.Repositories
{
    public class TenantSettingsRepositoryAsync : GenericRepositoryAsync<TenantSettings, Guid>, ITenantSettingsRepositoryAsync
    {
        private readonly ApplicationDbContext _context;

        public TenantSettingsRepositoryAsync(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<TenantSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            return _context.TenantSettings
                .SingleOrDefaultAsync(ts => ts.TenantId == tenantId, cancellationToken);
        }
    }
}
