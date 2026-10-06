using Pos.tenant.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.Tenants.DTOS
{
    public class TenantUsageUpdateResult
    {
        public Guid TenantId { get; set; }

        public TenantUsageCounterType CounterType { get; set; }

        public int UsedCount { get; set; }

        public int? MaxAllowed { get; set; }
    }
}
