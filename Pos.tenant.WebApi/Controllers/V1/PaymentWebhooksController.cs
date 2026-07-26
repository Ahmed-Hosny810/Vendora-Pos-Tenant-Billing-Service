using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.tenant.Application.DTOS;
using Pos.tenant.Application.Features.SubscriptionPayments.Commands.HandlePaymobWebhook;

namespace Pos.tenant.WebApi.Controllers.V1
{
    [Route("api/v{version:apiVersion}/payment-webhooks")]
    [ApiController]
    [ApiVersion("1.0")]
    public class PaymentWebhooksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentWebhooksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("paymob")]
        public async Task<IActionResult> HandlePaymobWebhook(
            [FromQuery(Name = "hmac")] string? hmac,
            [FromBody] PaymobWebhookRequest request)
        {
            var receivedHmac = hmac;

            if (request.Obj == null)
            {
                return BadRequest(new
                {
                    message = "Invalid Paymob webhook payload."
                });
            }

            var result = await _mediator.Send(new HandlePaymobWebhookCommand
            {
                Payload = request,
                Hmac = receivedHmac
            });

            if (result.IsFailure)
            {
                return BadRequest(new
                {
                    message = "Paymob webhook processing failed.",
                    errors = result.Errors
                });
            }

            return Ok(new
            {
                message = "Paymob webhook processed successfully.",
                paymentId = result.Value
            });
        }
    }
}
