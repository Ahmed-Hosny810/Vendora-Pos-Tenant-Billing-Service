using Pos.tenant.Domain.Models;


namespace Pos.tenant.Application.Interfaces.Repositories
{
    public interface ITenantSubscriptionRepositoryAsync : IGenericRepositoryAsync<TenantSubscription, Guid>
    {
        /// <summary>Gets the latest subscription and plan for billing, regardless of status.</summary>
        Task<TenantSubscription?> GetLatestSubscriptionByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>Gets the active subscription and plan whose period contains the supplied UTC time.</summary>
        Task<TenantSubscription?> GetActiveSubscriptionByTenantIdAsync(Guid tenantId, DateTime nowUtc, CancellationToken cancellationToken = default);
        Task<TenantSubscription?> GetSubscriptionAndPlanByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a limited batch of active subscriptions expiring on or before the cutoff
        /// that do not already have an invoice for their current expiry date.
        /// </summary>
        /// <param name="renewBeforeUtc">The UTC expiry cutoff, usually UTC now plus seven days.</param>
        /// <param name="batchSize">The maximum number of subscription IDs to return.</param>
        /// <param name="cancellationToken">Token used to cancel the database query.</param>
        Task<IReadOnlyList<Guid>> GetSubscriptionIdsDueForRenewalAsync(DateTime renewBeforeUtc, int batchSize, CancellationToken cancellationToken);


        Task<IReadOnlyList<Guid>> GetSubscriptionIdsRequiringExpiryProcessingAsync(DateTime nowUtc,int batchSize,CancellationToken cancellationToken);

        Task<TenantSubscription?> GetForExpiryAsync(Guid subscriptionId,CancellationToken cancellationToken);

        Task<bool> HasActiveSubscriptionAsync(Guid tenantId,Guid excludedSubscriptionId,DateTime nowUtc,CancellationToken cancellationToken);
    }
}
