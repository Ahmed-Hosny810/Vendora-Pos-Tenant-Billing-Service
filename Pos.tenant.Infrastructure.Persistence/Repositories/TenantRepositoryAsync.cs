using Microsoft.EntityFrameworkCore;
using Pos.tenant.Application.Features.Tenants.Queries.GetAllQuery;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Models;
using Pos.tenant.Infrastructure.Persistence.Contexts;
using Pos.tenant.Infrastructure.Persistence.QueryExtensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Infrastructure.Persistence.Repositories
{
    public class TenantRepositoryAsync : GenericRepositoryAsync<Tenant, Guid>, ITenantRepositoryAsync
    {
        private readonly ApplicationDbContext _context;

        public TenantRepositoryAsync(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Tenant?> GetTenantByIdAsync(Guid tenantId, TenantIncludes includes)
        {
            return await _context.Tenants
                .AsNoTracking()
                .ApplyIncludes(includes)
                .FirstOrDefaultAsync(t => t.Id == tenantId);
        }

        public Task<Tenant?> GetByCreatorUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return _context.Tenants.SingleOrDefaultAsync(tenant => tenant.CreatedByUserId == userId, cancellationToken);
        }

        public async Task<PagedResponse<IEnumerable<Tenant>>> GetTenantsPagedResponseAsync(TenantFilter filter, TenantIncludes includes, TenantOrderKey orderKey, bool orderDescending, int pageNumber, int pageSize)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var query = _context.Tenants.AsNoTracking();

            var totalRecords = await query
                .ApplyFilters(filter)
                .CountAsync();

            var tenants = await query
                .ApplyFilters(filter)
                .ApplyIncludes(includes)
                .ApplyOrdering(orderKey, orderDescending)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<IEnumerable<Tenant>>(
                tenants,
                pageNumber,
                pageSize,
                totalRecords);
        }
    }
}
