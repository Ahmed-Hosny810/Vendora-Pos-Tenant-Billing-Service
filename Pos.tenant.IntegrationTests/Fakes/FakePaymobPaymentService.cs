using Pos.tenant.Application.Features.SubscriptionPayments.DTOS;
using Pos.tenant.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.IntegrationTests.Fakes
{
    public class FakePaymobPaymentService : IPaymobPaymentService
    {
        public string BuildCheckoutUrl(string clientSecret)
        {
            return $"https://fake-paymob-checkout.test/reused/{clientSecret}";
        }

        public Task<PaymobCreateIntentionResult> CreateIntentionAsync(PaymobCreateIntentionRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PaymobCreateIntentionResult
            {
                ClientSecret = $"fake_client_secret_{request.PaymentId}",
                ProviderPaymentReference = $"fake_paymob_reference_{request.PaymentId}",
                ProviderStatus = "intended",
                CheckoutUrl = $"https://fake-paymob-checkout.test/{request.PaymentId}"
            });
        }
    }
}
