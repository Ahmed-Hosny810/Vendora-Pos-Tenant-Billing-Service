using Pos.tenant.Application.DTOS;


namespace Pos.tenant.Application.Interfaces.Payment
{
    public interface IPaymobWebhookVerifier
    {
        PaymobWebhookResult VerifyAndParse(
            PaymobWebhookRequest request,
            string? hmac);
    }
}
