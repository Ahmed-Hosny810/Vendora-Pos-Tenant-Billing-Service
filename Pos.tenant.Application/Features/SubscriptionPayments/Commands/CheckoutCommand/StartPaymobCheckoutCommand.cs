using MediatR;
using Microsoft.Extensions.Logging;
using Pos.tenant.Application.Features.SubscriptionPayments.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.SubscriptionPayments.Commands.CheckoutCommand
{
    public class StartPaymobCheckoutCommand : IRequest<Result<PaymobCheckoutDto>>
    {
        public Guid InvoiceId { get; set; }

        public string IdempotencyKey { get; set; } = null!;
    }
    public class StartPaymobCheckoutCommandHandler: IRequestHandler<StartPaymobCheckoutCommand, Result<PaymobCheckoutDto>>
    {
        private readonly ISubscriptionInvoiceRepositoryAsync _subscriptionInvoiceRepository;
        private readonly ISubscriptionPaymentRepositoryAsync _subscriptionPaymentRepository;
        private readonly IPaymobPaymentService _paymobPaymentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<StartPaymobCheckoutCommandHandler> _logger;

        public StartPaymobCheckoutCommandHandler(
            ISubscriptionInvoiceRepositoryAsync subscriptionInvoiceRepository,
            ISubscriptionPaymentRepositoryAsync subscriptionPaymentRepository,
            IPaymobPaymentService paymobPaymentService,
            IUnitOfWork unitOfWork,
            ILogger<StartPaymobCheckoutCommandHandler> logger,
            ICurrentUserService currentUser)
        {
            _subscriptionInvoiceRepository = subscriptionInvoiceRepository;
            _subscriptionPaymentRepository = subscriptionPaymentRepository;
            _paymobPaymentService = paymobPaymentService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result<PaymobCheckoutDto>> Handle(StartPaymobCheckoutCommand request, CancellationToken cancellationToken)
        {
            
            var idempotencyKey = request.IdempotencyKey.Trim();

            var tenantId = _currentUser.TenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            var invoice = await _subscriptionInvoiceRepository.GetByTenantAndIdAsync(
                tenantId.Value, request.InvoiceId, cancellationToken);

            if (invoice == null)
                return Result<PaymobCheckoutDto>.Failure("Subscription invoice not found.");

            var existingPayment = await _subscriptionPaymentRepository.GetByIdempotencyKeyAsync(tenantId.Value, idempotencyKey, cancellationToken);

            if (existingPayment != null)
            {
                if (existingPayment.InvoiceId != request.InvoiceId)
                {
                    return Result<PaymobCheckoutDto>.Failure(
                        "This idempotency key was already used for a different invoice.");
                }

                if (existingPayment.Status == PaymentStatuses.Completed)
                {
                    return Result<PaymobCheckoutDto>.Failure(
                        "This payment has already been completed.");
                }

                if (existingPayment.Status == PaymentStatuses.Failed)
                {
                    return Result<PaymobCheckoutDto>.Failure(
                        "This checkout attempt has already failed. Please start a new checkout with a new idempotency key.");
                }

                if (existingPayment.Status == PaymentStatuses.Refunded)
                {
                    return Result<PaymobCheckoutDto>.Failure(
                        "This payment has already been refunded. Please start a new checkout with a new idempotency key if needed.");
                }

                if (string.IsNullOrWhiteSpace(existingPayment.ProviderClientSecret))
                {
                    return Result<PaymobCheckoutDto>.Failure(
                        "Checkout is already being processed. Please try again shortly.");
                }

                var existingCheckoutUrl= _paymobPaymentService.BuildCheckoutUrl(existingPayment.ProviderClientSecret);
                
                _logger.LogInformation("Returning existing checkout URL for payment {PaymentId}.", existingPayment.Id);
    
                return Result<PaymobCheckoutDto>.Success(new PaymobCheckoutDto
                {
                    PaymentId = existingPayment.Id,
                    InvoiceId = existingPayment.InvoiceId,
                    Provider = existingPayment.Provider!,
                    Status = existingPayment.Status,
                    CheckoutUrl = existingCheckoutUrl
                });
            }
            if (invoice.Status == InvoiceStatuses.Cancelled)
                return Result<PaymobCheckoutDto>.Failure("Cannot start checkout for a cancelled invoice.");

            if (invoice.Status == InvoiceStatuses.Paid)
                return Result<PaymobCheckoutDto>.Failure("Invoice is already paid.");

            _logger.LogInformation("Starting Paymob checkout for invoice {InvoiceId} with idempotency key {IdempotencyKey}.", invoice.Id, idempotencyKey);

            var payment = new SubscriptionPayment
            {
                Id = Guid.NewGuid(),
                TenantId = invoice.TenantId,
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Method = PaymentMethods.OnlineCard,
                Provider = PaymentProviders.Paymob,
                Status = PaymentStatuses.Pending,
                PaidAt = null,
                IdempotencyKey = idempotencyKey
            };

            await _subscriptionPaymentRepository.AddAsync(payment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var paymobResult = await _paymobPaymentService.CreateIntentionAsync(
                new PaymobCreateIntentionRequest
                {
                    PaymentId = payment.Id,
                    InvoiceId = invoice.Id.ToString(),
                    InvoiceNumber = invoice.InvoiceNumber,
                    Amount = invoice.Total,
                    Currency = "EGP"
                },
                cancellationToken);

            _logger.LogInformation(
             "Paymob intention created for payment {PaymentId}. ProviderPaymentReference: {ProviderPaymentReference}", payment.Id,
             paymobResult.ProviderPaymentReference);

            payment.ProviderClientSecret = paymobResult.ClientSecret;
            payment.ProviderPaymentReference = paymobResult.ProviderPaymentReference;
            payment.ProviderStatus = paymobResult.ProviderStatus ?? "intended";

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<PaymobCheckoutDto>.Success(new PaymobCheckoutDto
            {
                PaymentId = payment.Id,
                InvoiceId = payment.InvoiceId,
                Provider = payment.Provider,
                Status = payment.Status,
                CheckoutUrl = paymobResult.CheckoutUrl
            });

        }
    }
}
