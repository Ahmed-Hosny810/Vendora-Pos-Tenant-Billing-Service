using MediatR;
using Pos.tenant.Application.Features.SubscriptionInvoices.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.SubscriptionInvoices.Commands.CreateCommand
{
    public class CreateSubscriptionInvoiceCommand :IRequest<Result<SubscriptionInvoiceDto>>
    {
        public Guid TenantId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public DateTime DueDate { get; set; }
    }
    public class CreateSubscriptionInvoiceCommandHandler
        : IRequestHandler<CreateSubscriptionInvoiceCommand, Result<SubscriptionInvoiceDto>>
    {
        private readonly ITenantRepositoryAsync _tenantRepository;
        private readonly ITenantSubscriptionRepositoryAsync _tenantSubscriptionRepository;
        private readonly ISubscriptionInvoiceRepositoryAsync _subscriptionInvoiceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateSubscriptionInvoiceCommandHandler(
            ITenantRepositoryAsync tenantRepository,
            ITenantSubscriptionRepositoryAsync tenantSubscriptionRepository,
            ISubscriptionInvoiceRepositoryAsync subscriptionInvoiceRepository,
            IUnitOfWork unitOfWork)
        {
            _tenantRepository = tenantRepository;
            _tenantSubscriptionRepository = tenantSubscriptionRepository;
            _subscriptionInvoiceRepository = subscriptionInvoiceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<SubscriptionInvoiceDto>> Handle(CreateSubscriptionInvoiceCommand request,CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdAsync(request.TenantId);

            if (tenant == null)
                return Result<SubscriptionInvoiceDto>.Failure("Tenant not found.");

            var subscription = await _tenantSubscriptionRepository
                .GetLatestSubscriptionByTenantIdAsync(request.TenantId, cancellationToken);

            if (subscription == null)
                return Result<SubscriptionInvoiceDto>.Failure("Tenant subscription not found.");

            if (subscription.Plan == null)
                return Result<SubscriptionInvoiceDto>.Failure("Subscription plan not found.");

            if (subscription.Status == TenantSubscriptionStatuses.Cancelled ||subscription.Status == TenantSubscriptionStatuses.Expired)
            {
                return Result<SubscriptionInvoiceDto>.Failure(
                    "Cannot create invoice for cancelled or expired subscription.");
            }

            var invoiceNumber = await GenerateUniqueInvoiceNumberAsync(request.TenantId);

            var invoice = new SubscriptionInvoice
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                TenantSubscriptionId = subscription.Id,
                InvoiceNumber = invoiceNumber,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                DueDate = request.DueDate,
                Total = subscription.Plan.MonthlyPrice,
                Status = InvoiceStatuses.Unpaid,
                PaidAt = null
            };

            await _subscriptionInvoiceRepository.AddAsync(invoice);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = new SubscriptionInvoiceDto
            {
                Id = invoice.Id,
                TenantId = invoice.TenantId,
                TenantSubscriptionId = invoice.TenantSubscriptionId,
                InvoiceNumber = invoice.InvoiceNumber,
                PeriodStart = invoice.PeriodStart,
                PeriodEnd = invoice.PeriodEnd,
                DueDate = invoice.DueDate,
                Total = invoice.Total,
                Status = invoice.Status,
                PaidAt = invoice.PaidAt
            };

            return Result<SubscriptionInvoiceDto>.Success(dto);
        }

        private async Task<string> GenerateUniqueInvoiceNumberAsync(Guid tenantId)
        {
            string invoiceNumber;

            do
            {
                var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

                invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
            }
            while (await _subscriptionInvoiceRepository
                .IsInvoiceNumberExistsAsync(tenantId, invoiceNumber));

            return invoiceNumber;
        }
    }
}
