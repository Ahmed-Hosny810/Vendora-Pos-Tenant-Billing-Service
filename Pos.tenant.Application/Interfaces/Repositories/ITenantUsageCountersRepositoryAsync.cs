using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Interfaces.Repositories
{
    public interface ITenantUsageCountersRepositoryAsync : IGenericRepositoryAsync<TenantUsageCounters, Guid>
    {
        Task<TenantUsageCounters?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    }
}
