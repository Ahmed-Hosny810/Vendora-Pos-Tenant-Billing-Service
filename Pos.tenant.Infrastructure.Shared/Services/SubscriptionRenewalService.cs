
using Microsoft.Extensions.Options;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;
using Pos.tenant.Infrastructure.Shared.Constants;

namespace Pos.tenant.Infrastructure.Shared.Services
{
    public class SubscriptionRenewalService : ISubscriptionRenewalService
    {
        private readonly ITenantSubscriptionRepositoryAsync _subscriptionRepository;
        private readonly ISubscriptionInvoiceRepositoryAsync _invoiceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SubscriptionRenewalOptions _options;

        public SubscriptionRenewalService(
            ITenantSubscriptionRepositoryAsync subscriptionRepository,
            ISubscriptionInvoiceRepositoryAsync invoiceRepository,
            IUnitOfWork unitOfWork,
            IOptions<SubscriptionRenewalOptions> options)
        {
            _subscriptionRepository = subscriptionRepository;
            _invoiceRepository = invoiceRepository;
            _unitOfWork = unitOfWork;
            _options = options.Value;
        }

        public async Task<Result<Guid>> CreateRenewalInvoiceAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken)
        {
            // 1. Load the subscription and the plan used to price the renewal.
            var subscription =
                await _subscriptionRepository.GetSubscriptionAndPlanByIdAsync(
                    subscriptionId,
                    cancellationToken);

            if (subscription == null)
                return Result<Guid>.Failure("Subscription was not found.");

            // Expiry may run before renewal generation after an application outage.
            if (subscription.Status != TenantSubscriptionStatuses.Active &&
                subscription.Status != TenantSubscriptionStatuses.Expired)
                return Result<Guid>.Failure(
                    "Only active or expired subscriptions can receive renewal invoices.");

            if (!subscription.CurrentPeriodEnd.HasValue)
                return Result<Guid>.Failure(
                    "The subscription does not have an expiry date.");

            var dueDate = subscription.CurrentPeriodEnd.Value;

            // 2. Return an invoice already created for this renewal.
            var existingInvoice =
                await _invoiceRepository.GetBySubscriptionAndDueDateAsync(
                    subscription.TenantId,
                    subscription.Id,
                    dueDate,
                    cancellationToken);

            if (existingInvoice != null)
                return Result<Guid>.Success(existingInvoice.Id);

            // 3. Confirm that the subscription is within the renewal window.
            var now = DateTime.UtcNow;
            var expiryCutoffUtc = now.AddDays(_options.AdvanceDays);

            if (dueDate > expiryCutoffUtc)
                return Result<Guid>.Failure(
                    "The subscription is not due for renewal yet.");

            if (subscription.Plan == null)
                return Result<Guid>.Failure(
                    "The subscription plan was not found.");

            // 4. Create the unpaid invoice without extending the subscription.
            var invoiceId = Guid.NewGuid();

            var invoice = new SubscriptionInvoice
            {
                Id = invoiceId,
                TenantId = subscription.TenantId,
                TenantSubscriptionId = subscription.Id,
                InvoiceNumber = $"INV-{invoiceId:N}",
                Total = subscription.Plan.MonthlyPrice,
                Status = InvoiceStatuses.Unpaid,
                DueDate = dueDate,
                PeriodStart = dueDate,
                PeriodEnd = dueDate.AddMonths(1),
                CreatedAt = now
            };

            await _invoiceRepository.AddAsync(invoice);

            // 5. Persist the invoice. Activation happens after payment.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(invoice.Id);
        }
    }
}
