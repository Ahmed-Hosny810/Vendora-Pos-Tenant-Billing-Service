using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.Tenants.DTOS
{
    public class IncreaseUsageRequest
    {
        public Guid TenantId { get; set; }
    }
}

