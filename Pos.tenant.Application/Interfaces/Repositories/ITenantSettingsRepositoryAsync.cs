using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Interfaces.Repositories
{
    public interface ITenantSettingsRepositoryAsync : IGenericRepositoryAsync<TenantSettings, Guid>
    {
        Task<TenantSettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    }
}
