using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;


namespace Pos.tenant.Infrastructure.Shared.Services
{
    public class TenantSubscriptionActivationService : ITenantSubscriptionActivationService
    {
        private readonly ITenantRepositoryAsync _tenantRepository;
        private readonly ITenantSubscriptionRepositoryAsync _tenantSubscriptionRepository;
        private readonly ITenantStatusHistoryRepositoryAsync _tenantStatusHistoryRepository;
        private readonly ISubscriptionInvoiceRepositoryAsync _invoiceRepository;

        public TenantSubscriptionActivationService(
            ITenantRepositoryAsync tenantRepository,
            ITenantSubscriptionRepositoryAsync tenantSubscriptionRepository,
            ITenantStatusHistoryRepositoryAsync tenantStatusHistoryRepository,
            ISubscriptionInvoiceRepositoryAsync invoiceRepository)
        {
            _tenantRepository = tenantRepository;
            _tenantSubscriptionRepository = tenantSubscriptionRepository;
            _tenantStatusHistoryRepository = tenantStatusHistoryRepository;
            _invoiceRepository = invoiceRepository;
        }

        public async Task<Result<Guid>> ActivateAfterInvoicePaidAsync(
            SubscriptionInvoice invoice,
            CancellationToken cancellationToken = default)
        {
            if (invoice.Status != InvoiceStatuses.Paid)
                return Result<Guid>.Failure("Invoice must be paid before activating subscription.");

            if (!invoice.PaidAt.HasValue)
                return Result<Guid>.Failure("The paid invoice must have a payment date.");

            // Both payment paths save the payment completion time on the invoice.
            var periodStart = invoice.PaidAt.Value;
            var periodEnd = periodStart.AddMonths(1);

            var tenant = await _tenantRepository.GetByIdAsync(invoice.TenantId);

            if (tenant == null)
                return Result<Guid>.Failure("Tenant not found for paid invoice.");

            var subscription = await _tenantSubscriptionRepository.GetByIdAsync(
                invoice.TenantSubscriptionId);

            if (subscription == null)
                return Result<Guid>.Failure("Subscription not found for paid invoice.");

            if (subscription.Status == TenantSubscriptionStatuses.Cancelled)
                return Result<Guid>.Failure("Cannot activate a cancelled subscription.");

            
            if (subscription.CurrentPeriodEnd.HasValue &&
                subscription.CurrentPeriodEnd.Value >= periodEnd &&
                subscription.Status == TenantSubscriptionStatuses.Active)
            {
                return Result<Guid>.Success(subscription.Id);
            }

            var oldTenantStatus = tenant.Status;

            // Load a tracked invoice because webhook retries may supply a detached one.
            var savedInvoice = await _invoiceRepository.GetByIdAsync(invoice.Id);
            if (savedInvoice == null)
                return Result<Guid>.Failure("Invoice not found for activation.");

            savedInvoice.PeriodStart = periodStart;
            savedInvoice.PeriodEnd = periodEnd;

            subscription.MarkActive(periodStart, periodEnd);

            if (tenant.Status != TenantStatuses.Active)
            {
                tenant.Activate();

                var statusHistory = new TenantStatusHistory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    OldStatus = oldTenantStatus,
                    NewStatus = TenantStatuses.Active,
                    Reason = $"Tenant activated after paid invoice {invoice.InvoiceNumber}.",
                    ChangedAt = DateTime.UtcNow
                };

                await _tenantStatusHistoryRepository.AddAsync(statusHistory);
            }

            _tenantRepository.Update(tenant);
            _tenantSubscriptionRepository.Update(subscription);

            return Result<Guid>.Success(subscription.Id);
        }
    }
}
