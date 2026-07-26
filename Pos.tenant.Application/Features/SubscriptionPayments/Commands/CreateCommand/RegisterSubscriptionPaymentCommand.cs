using MediatR;
using Pos.tenant.Application.Features.SubscriptionPayments.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;

namespace Pos.tenant.Application.Features.SubscriptionPayments.Commands.CreateCommand
{
    public class RegisterSubscriptionPaymentCommand:IRequest<Result<SubscriptionPaymentDto>>
    {
        public Guid InvoiceId { get; set; }

        public decimal Amount { get; set; }

        public string Method { get; set; } = null!;

        public string? ReferenceNumber { get; set; }

        public bool MarkAsCompleted { get; set; }
    }

    public class RegisterSubscriptionPaymentCommandHandler: IRequestHandler<RegisterSubscriptionPaymentCommand, Result<SubscriptionPaymentDto>>
    {
        private readonly ISubscriptionPaymentRepositoryAsync _subscriptionPaymentRepository;
        private readonly ISubscriptionInvoiceRepositoryAsync _subscriptionInvoiceRepository;
        private readonly ITenantSubscriptionActivationService _tenantSubscriptionActivationService;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterSubscriptionPaymentCommandHandler(
            ISubscriptionPaymentRepositoryAsync subscriptionPaymentRepository,
            ISubscriptionInvoiceRepositoryAsync subscriptionInvoiceRepository,
            ITenantSubscriptionActivationService tenantSubscriptionActivationService,
            IUnitOfWork unitOfWork)
        {
            _subscriptionPaymentRepository = subscriptionPaymentRepository;
            _subscriptionInvoiceRepository = subscriptionInvoiceRepository;
            _tenantSubscriptionActivationService = tenantSubscriptionActivationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<SubscriptionPaymentDto>> Handle(
            RegisterSubscriptionPaymentCommand request,
            CancellationToken cancellationToken)
        {
            var invoice = await _subscriptionInvoiceRepository.GetByIdAsync(
                request.InvoiceId);

            if (invoice == null)
                return Result<SubscriptionPaymentDto>.Failure("Invoice not found.");

            if (invoice.Status == InvoiceStatuses.Paid)
                return Result<SubscriptionPaymentDto>.Failure("Invoice is already paid.");

            if (invoice.Status == InvoiceStatuses.Cancelled)
                return Result<SubscriptionPaymentDto>.Failure("Cannot pay a cancelled invoice.");

            if (request.Amount <= 0)
                return Result<SubscriptionPaymentDto>.Failure("Payment amount must be greater than zero.");

            if (request.MarkAsCompleted && request.Amount != invoice.Total)
            {
                return Result<SubscriptionPaymentDto>.Failure(
                    "Completed payment amount must equal invoice total.");
            }

            var now = DateTime.UtcNow;

            var payment = new SubscriptionPayment
            {
                Id = Guid.NewGuid(),
                TenantId = invoice.TenantId,
                InvoiceId = invoice.Id,
                Amount = request.Amount,
                Method = request.Method.Trim(),
                ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber)
                    ? null
                    : request.ReferenceNumber.Trim(),

                Provider = PaymentProviders.Manual,
                Status = PaymentStatuses.Pending,
                PaidAt = null
            };

            if (request.MarkAsCompleted)
            {
                payment.MarkCompleted(now);
                invoice.MarkPaid(now);
            }

            await _subscriptionPaymentRepository.AddAsync(payment);

              //  Save payment + invoice first.

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (request.MarkAsCompleted)
            {
                var activationResult =
                    await _tenantSubscriptionActivationService.ActivateAfterInvoicePaidAsync(
                        invoice,
                        cancellationToken);

                if (activationResult.IsFailure)
                {
                    return Result<SubscriptionPaymentDto>.Failure(
                        activationResult.Errors.ToArray());
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var dto = new SubscriptionPaymentDto
            {
                PaymentId = payment.Id,
                TenantId = payment.TenantId,
                InvoiceId = payment.InvoiceId,
                Amount = payment.Amount,
                Method = payment.Method,
                ReferenceNumber = payment.ReferenceNumber,
                Provider = payment.Provider,
                Status = payment.Status,
                PaidAt = payment.PaidAt
            };

            return Result<SubscriptionPaymentDto>.Success(dto);
        }
    }
}