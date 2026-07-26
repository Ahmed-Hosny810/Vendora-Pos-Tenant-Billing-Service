using MediatR;
using Microsoft.Extensions.Logging;
using Pos.tenant.Application.DTOS;
using Pos.tenant.Application.Interfaces.Payment;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.SubscriptionPayments.Commands.HandlePaymobWebhook
{
    public class HandlePaymobWebhookCommand : IRequest<Result<Guid>>
    {
        public PaymobWebhookRequest Payload { get; set; } = null!;

        public string? Hmac { get; set; }
    }

    public class HandlePaymobWebhookCommandHandler: IRequestHandler<HandlePaymobWebhookCommand, Result<Guid>>
    {
        private readonly IPaymobWebhookVerifier _paymobWebhookVerifier;
        private readonly ISubscriptionPaymentRepositoryAsync _subscriptionPaymentRepository;
        private readonly ISubscriptionInvoiceRepositoryAsync _subscriptionInvoiceRepository;
        private readonly ITenantSubscriptionActivationService _tenantSubscriptionActivationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HandlePaymobWebhookCommandHandler> _logger;

        public HandlePaymobWebhookCommandHandler(
            IPaymobWebhookVerifier paymobWebhookVerifier,
            ISubscriptionPaymentRepositoryAsync subscriptionPaymentRepository,
            ISubscriptionInvoiceRepositoryAsync subscriptionInvoiceRepository,
            ITenantSubscriptionActivationService tenantSubscriptionActivationService,
            IUnitOfWork unitOfWork,
            ILogger<HandlePaymobWebhookCommandHandler> logger)
        {
            _paymobWebhookVerifier = paymobWebhookVerifier;
            _subscriptionPaymentRepository = subscriptionPaymentRepository;
            _subscriptionInvoiceRepository = subscriptionInvoiceRepository;
            _tenantSubscriptionActivationService = tenantSubscriptionActivationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(HandlePaymobWebhookCommand request,
            CancellationToken cancellationToken)
        {
            PaymobWebhookResult webhookResult;

            try
            {
                webhookResult = _paymobWebhookVerifier.VerifyAndParse(request.Payload, request.Hmac);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Paymob webhook verification failed.");

                return Result<Guid>.Failure(ex.Message);
            }

            _logger.LogInformation(
                "Paymob webhook verified. ProviderPaymentReference: {ProviderPaymentReference}, ProviderTransactionId: {ProviderTransactionId}, Success: {Success}, Pending: {Pending}, AmountCents: {AmountCents}, Currency: {Currency}",
                webhookResult.ProviderPaymentReference,
                webhookResult.ProviderTransactionId,
                webhookResult.Success,
                webhookResult.Pending,
                webhookResult.AmountCents,
                webhookResult.Currency);

            var payment = await _subscriptionPaymentRepository
                .GetByProviderPaymentReferenceWithInvoiceAsync(
                    webhookResult.ProviderPaymentReference,
                    cancellationToken);

            if (payment == null)
            {
                _logger.LogError(
                    "Subscription payment not found for Paymob ProviderPaymentReference: {ProviderPaymentReference}",
                    webhookResult.ProviderPaymentReference);

                return Result<Guid>.Failure("Subscription payment not found.");
            }

            if (payment.Invoice == null)
            {
                _logger.LogError(
                    "Related invoice not found for PaymentId: {PaymentId}, ProviderPaymentReference: {ProviderPaymentReference}",
                    payment.Id,
                    webhookResult.ProviderPaymentReference);

                return Result<Guid>.Failure("Related invoice not found.");
            }

            /*
                Important:
                If payment is already completed, do not just return success.
                Try activation again in case the previous webhook saved the payment
                but failed during tenant/subscription activation.
            */
            if (payment.Status == PaymentStatuses.Completed)
            {
                _logger.LogInformation(
                    "Payment already completed. Ensuring activation. PaymentId: {PaymentId}, InvoiceId: {InvoiceId}",
                    payment.Id,
                    payment.InvoiceId);

                return await EnsureActivationAfterCompletedPaymentAsync(
                    payment,
                    cancellationToken);
            }
            /*
                Pending means Paymob has not confirmed final success/failure yet.
                We only update provider tracking data.
            */
            if (webhookResult.Pending)
            {
                payment.MarkPending(webhookResult.ProviderTransactionId, webhookResult.ProviderStatus);

                _subscriptionPaymentRepository.Update(payment);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Paymob payment is still pending. PaymentId: {PaymentId}, InvoiceId: {InvoiceId}, ProviderTransactionId: {ProviderTransactionId}",
                    payment.Id,
                    payment.InvoiceId,
                    payment.ProviderTransactionId);

                return Result<Guid>.Success(payment.Id);
            }

            /*
                Successful payment:
                Save payment + invoice first.
                Then activate tenant/subscription.
            */
            if (webhookResult.Success)
            {
                var validationResult = ValidateSuccessfulPayment(
                    payment,
                    webhookResult);

                if (validationResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Successful Paymob webhook validation failed. PaymentId: {PaymentId}, InvoiceId: {InvoiceId}, Errors: {Errors}",
                        payment.Id,
                        payment.InvoiceId,
                        string.Join(" | ", validationResult.Errors));

                    return validationResult;
                }

                var now = DateTime.UtcNow;

                payment.MarkCompleted(
                    now,
                    webhookResult.ProviderTransactionId);

                payment.ProviderStatus = webhookResult.ProviderStatus;

                payment.Invoice.MarkPaid(now);

                _subscriptionPaymentRepository.Update(payment);
                _subscriptionInvoiceRepository.Update(payment.Invoice);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Payment and invoice marked as paid. PaymentId: {PaymentId}, InvoiceId: {InvoiceId}, TenantId: {TenantId}, ProviderTransactionId: {ProviderTransactionId}",
                    payment.Id,
                    payment.InvoiceId,
                    payment.TenantId,
                    payment.ProviderTransactionId);

                return await EnsureActivationAfterCompletedPaymentAsync(
                    payment,
                    cancellationToken);
            }

            /*
                Failed payment:
                Payment becomes Failed.
                Invoice remains Unpaid.
                Tenant/subscription remain Pending.
            */
            payment.MarkFailed(webhookResult.FailureReason, webhookResult.ProviderTransactionId);

            payment.ProviderStatus = webhookResult.ProviderStatus;

            _subscriptionPaymentRepository.Update(payment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Paymob payment failed. PaymentId: {PaymentId}, InvoiceId: {InvoiceId}, ProviderTransactionId: {ProviderTransactionId}, FailureReason: {FailureReason}",
                payment.Id,
                payment.InvoiceId,
                payment.ProviderTransactionId,
                payment.FailureReason);

            return Result<Guid>.Success(payment.Id);
        }

        private async Task<Result<Guid>> EnsureActivationAfterCompletedPaymentAsync(
            SubscriptionPayment payment,
            CancellationToken cancellationToken)
        {
            if (payment.Invoice == null)
            {
                _logger.LogError(
                    "Cannot activate tenant subscription because invoice is missing. PaymentId: {PaymentId}",
                    payment.Id);

                return Result<Guid>.Failure("Related invoice not found.");
            }

            if (payment.Invoice.Status != InvoiceStatuses.Paid)
            {
                _logger.LogError(
                    "Cannot activate tenant subscription because invoice is not paid. PaymentId: {PaymentId}, InvoiceId: {InvoiceId}, InvoiceStatus: {InvoiceStatus}",
                    payment.Id,
                    payment.InvoiceId,
                    payment.Invoice.Status);

                return Result<Guid>.Failure("Completed payment invoice is not marked as paid.");
            }

            var activationResult =
                await _tenantSubscriptionActivationService.ActivateAfterInvoicePaidAsync(
                    payment.Invoice,
                    cancellationToken);

            if (activationResult.IsFailure)
            {
                _logger.LogError(
                    "Tenant subscription activation failed after successful payment. PaymentId: {PaymentId}, InvoiceId: {InvoiceId}, TenantId: {TenantId}, Errors: {Errors}",
                    payment.Id,
                    payment.InvoiceId,
                    payment.TenantId,
                    string.Join(" | ", activationResult.Errors));

                return Result<Guid>.Failure(activationResult.Errors.ToArray());
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Tenant subscription activated successfully after payment. PaymentId: {PaymentId}, InvoiceId: {InvoiceId}, TenantId: {TenantId}, SubscriptionId: {SubscriptionId}",
                payment.Id,
                payment.InvoiceId,
                payment.TenantId,
                activationResult.Value);

            return Result<Guid>.Success(payment.Id);
        }

        private Result<Guid> ValidateSuccessfulPayment(
            SubscriptionPayment payment,
            PaymobWebhookResult webhookResult)
        {
            if (payment.Invoice.Status == InvoiceStatuses.Cancelled)
            {
                return Result<Guid>.Failure(
                    "Cannot complete payment for a cancelled invoice.");
            }

            var expectedAmountCents = ConvertToCents(payment.Amount);

            if (webhookResult.AmountCents != expectedAmountCents)
            {
                return Result<Guid>.Failure(
                    "Webhook amount does not match payment amount.");
            }

            if (!string.Equals(
                    webhookResult.Currency,
                    "EGP",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Result<Guid>.Failure(
                    "Webhook currency does not match expected currency.");
            }

            return Result<Guid>.Success(payment.Id);
        }

        private static long ConvertToCents(decimal amount)
        {
            return (long)Math.Round(
                amount * 100,
                MidpointRounding.AwayFromZero);
        }
    }

}
