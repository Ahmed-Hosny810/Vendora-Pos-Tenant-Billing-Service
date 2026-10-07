
namespace Pos.tenant.Infrastructure.Shared.Constants
{
    public class SubscriptionRenewalOptions
    {
        public int AdvanceDays { get; set; } = 7;
        public int PollingIntervalSeconds { get; set; } = 3600;
        public int BatchSize { get; set; } = 100;
    }
}
