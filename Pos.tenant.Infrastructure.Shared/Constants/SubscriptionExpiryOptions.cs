
namespace Pos.tenant.Infrastructure.Shared.Constants
{
    public class SubscriptionExpiryOptions
    {
        public int PollingIntervalSeconds { get; set; } = 300;
        public int BatchSize { get; set; } = 100;
    }
}
