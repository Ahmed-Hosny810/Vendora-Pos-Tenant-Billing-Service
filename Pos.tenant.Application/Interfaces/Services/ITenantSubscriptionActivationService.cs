using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Interfaces.Services
{
    public interface ITenantSubscriptionActivationService
    {
        Task<Result<Guid>> ActivateAfterInvoicePaidAsync(
            SubscriptionInvoice invoice,
            CancellationToken cancellationToken = default);
    }
}
