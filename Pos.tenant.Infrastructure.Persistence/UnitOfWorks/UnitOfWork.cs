using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Infrastructure.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Pos.tenant.Infrastructure.Persistence.UnitOfWorks
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> TrySaveTenantCreationAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is SqlException sql &&
                (sql.Number == 2601 || sql.Number == 2627) &&
                sql.Message.Contains("IX_Tenants_CreatedByUserId", StringComparison.Ordinal))
            {
                _context.ChangeTracker.Clear();
                return false;
            }
        }
    }
}
