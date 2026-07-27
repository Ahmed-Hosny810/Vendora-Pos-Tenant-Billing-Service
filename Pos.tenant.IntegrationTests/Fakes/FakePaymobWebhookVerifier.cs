using Pos.tenant.Application.DTOS;
using Pos.tenant.Application.Interfaces.Payment;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.IntegrationTests.Fakes
{
    public class FakePaymobWebhookVerifier : IPaymobWebhookVerifier
    {
        public static PaymobWebhookResult NextResult { get; set; } = null!;

        public PaymobWebhookResult VerifyAndParse(PaymobWebhookRequest request,string? hmac)
        {
            if (NextResult == null)
                throw new InvalidOperationException("Fake Paymob webhook result was not configured.");

            return NextResult;
        }
    }
}
