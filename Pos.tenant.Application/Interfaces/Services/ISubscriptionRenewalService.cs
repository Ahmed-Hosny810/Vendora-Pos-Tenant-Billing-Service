using Pos.tenant.Application.Wrappers;

namespace Pos.tenant.Application.Interfaces.Services
{
    public interface ISubscriptionRenewalService
    {
        Task<Result<Guid>> CreateRenewalInvoiceAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken);
    }
}
