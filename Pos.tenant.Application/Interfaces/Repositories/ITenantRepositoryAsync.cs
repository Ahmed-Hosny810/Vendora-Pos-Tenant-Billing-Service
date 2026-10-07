using Pos.tenant.Application.Features.Tenants.Queries.GetAllQuery;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Pos.tenant.Application.Interfaces.Repositories
{
    public interface ITenantRepositoryAsync:IGenericRepositoryAsync<Tenant,Guid>
    {
        Task<PagedResponse<IEnumerable<Tenant>>> GetTenantsPagedResponseAsync(TenantFilter filter, TenantIncludes includes,
            TenantOrderKey orderKey, bool orderDescending, int currentPage, int pageSize);

        Task<Tenant?> GetTenantByIdAsync(Guid tenantId, TenantIncludes includes);
        Task<Tenant?> GetByCreatorUserIdAsync(Guid userId, CancellationToken cancellationToken);
    }
}
