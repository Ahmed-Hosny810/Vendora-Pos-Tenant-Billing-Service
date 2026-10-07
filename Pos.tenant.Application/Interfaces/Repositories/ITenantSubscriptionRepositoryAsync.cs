using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Interfaces.Repositories
{
    public interface ITenantSubscriptionRepositoryAsync : IGenericRepositoryAsync<TenantSubscription, Guid>
    {
        Task<TenantSubscription?> GetCurrentPlanByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    }
}
