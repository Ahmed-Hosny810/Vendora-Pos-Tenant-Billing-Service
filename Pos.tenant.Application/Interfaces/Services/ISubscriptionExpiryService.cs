
namespace Pos.tenant.Application.Interfaces.Services
{
    public interface ISubscriptionExpiryService
    {
        Task ProcessExpiryAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken);
    }

}
