using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;

namespace Pos.tenant.Infrastructure.Shared.Services
{
    public class SubscriptionExpiryService : ISubscriptionExpiryService
    {
        private readonly ITenantSubscriptionRepositoryAsync _subscriptionRepository;
        private readonly ITenantStatusHistoryRepositoryAsync _historyRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SubscriptionExpiryService(
            ITenantSubscriptionRepositoryAsync subscriptionRepository,
            ITenantStatusHistoryRepositoryAsync historyRepository,
            IUnitOfWork unitOfWork)
        {
            _subscriptionRepository = subscriptionRepository;
            _historyRepository = historyRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ProcessExpiryAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken)
        {
            // 1. Reload current records in this subscription's fresh scope.
            var subscription = await _subscriptionRepository.GetForExpiryAsync(
                subscriptionId,
                cancellationToken);

            if (subscription == null)
                return;

            var now = DateTime.UtcNow;

            // 2. Mark unpaid invoices overdue without changing their dates.
            foreach (var invoice in subscription.SubscriptionInvoices)
            {
                if (invoice.Status != InvoiceStatuses.Unpaid ||
                    invoice.DueDate >= now)
                {
                    continue;
                }

                invoice.MarkOverdue();
                invoice.UpdatedAt = now;
            }

            // 3. Expire only an active subscription whose period has ended.
            var shouldExpire =
                subscription.Status == TenantSubscriptionStatuses.Active &&
                subscription.CurrentPeriodEnd.HasValue &&
                subscription.CurrentPeriodEnd.Value <= now;

            if (shouldExpire)
            {
                subscription.Expire();
                subscription.UpdatedAt = now;

                // 4. Don't suspend access supplied by another active subscription.
                var hasAnotherActiveSubscription =
                    await _subscriptionRepository.HasActiveSubscriptionAsync(
                        subscription.TenantId,
                        subscription.Id,
                        now,
                        cancellationToken);

                var tenant = subscription.Tenant;

                if (!hasAnotherActiveSubscription &&
                    tenant.Status == TenantStatuses.Active)
                {
                    var oldStatus = tenant.Status;

                    tenant.Suspend();
                    tenant.UpdatedAt = now;

                    await _historyRepository.AddAsync(new TenantStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenant.Id,
                        OldStatus = oldStatus,
                        NewStatus = TenantStatuses.Suspended,
                        Reason = "Subscription period expired.",
                        ChangedAt = now
                    });
                }
            }

            // 5. Save invoice, subscription, tenant and history changes together.
              await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
