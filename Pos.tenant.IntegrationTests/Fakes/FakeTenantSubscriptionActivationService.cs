using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.IntegrationTests.Fakes
{
    public class FakeTenantSubscriptionActivationService : ITenantSubscriptionActivationService
    {
        public Task<Result<Guid>> ActivateAfterInvoicePaidAsync(
            SubscriptionInvoice invoice,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<Guid>.Success(invoice.TenantId));
        }
    }
}
