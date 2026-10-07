using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> TrySaveTenantCreationAsync(CancellationToken cancellationToken);
    }
}
