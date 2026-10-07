using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.tenant.Application.Features.SubscriptionPayments.Commands.CheckoutCommand;
using Pos.tenant.Application.Features.SubscriptionPayments.Commands.CreateCommand;
using Pos.tenant.Application.Features.SubscriptionPayments.DTOS;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.WebApi.Requests.Payments;
using Pos.tenant.Application.Features.SubscriptionPayments.Queries.GetAllQuery;
using Pos.tenant.Application.Features.SubscriptionPayments.Queries.GetByIdQuery;

namespace Pos.tenant.WebApi.Controllers.V1
{
    [Route("api/v{version:apiVersion}/payments")]
    [ApiController]
    [ApiVersion("1.0")]
    public class SubscriptionPaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SubscriptionPaymentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpGet]
        public async Task<ActionResult<PagedResponse<IEnumerable<SubscriptionPaymentDto>>>> GetPayments(
            [FromQuery] GetSubscriptionPaymentsQueryParameter parameter, CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new GetSubscriptionPaymentsQuery { Parameter = parameter }, cancellationToken));
        }

        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpGet("{paymentId:guid}")]
        public async Task<ActionResult<Response<SubscriptionPaymentDto>>> GetPaymentById(
            Guid paymentId, CancellationToken cancellationToken)
        {
            var payment = await _mediator.Send(new GetSubscriptionPaymentByIdQuery { PaymentId = paymentId }, cancellationToken);
            return Ok(new Response<SubscriptionPaymentDto>(payment));
        }

        // Manual registration is restricted to platform administrators.
        [HttpPost("manual")]
        [Authorize(Policy = "PlatformAdmins")]
        public async Task<ActionResult<Response<SubscriptionPaymentDto>>> RecordManualPayment([FromBody] RecordManualSubscriptionPaymentCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                return BadRequest(new Response<SubscriptionPaymentDto>
                {
                    Succeeded = false,
                    Message = "Subscription payment registration failed.",
                    Errors = result.Errors
                });
            }

            return Ok(
                new Response<SubscriptionPaymentDto>(
                    result.Value,
                    "Subscription payment registered successfully."
                ));
        }

        [HttpPost("paymob/checkout")]
        [Authorize(Policy = "TenantOwnerOnly")]
        public async Task<ActionResult<Response<PaymobCheckoutDto>>> StartPaymobCheckout( [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                        [FromBody] StartPaymobCheckoutRequest request)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return BadRequest(new Response<PaymobCheckoutDto>
                {
                    Succeeded = false,
                    Message = "Idempotency-Key header is required.",
                    Errors = new List<string>
                    {
                         "Please send a unique Idempotency-Key header for this checkout request."
                    }
                });
            }

            var command = new StartPaymobCheckoutCommand
            {
                InvoiceId = request.InvoiceId,
                IdempotencyKey = idempotencyKey.Trim()
            };

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                return BadRequest(new Response<PaymobCheckoutDto>
                {
                    Succeeded = false,
                    Message = "Paymob checkout creation failed.",
                    Errors = result.Errors
                });
            }

            return Ok(new Response<PaymobCheckoutDto>(
                result.Value,
                "Paymob checkout created successfully."
            ));
        }



    }
}
