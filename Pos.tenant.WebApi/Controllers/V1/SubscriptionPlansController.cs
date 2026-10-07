using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.tenant.Application.Features.SubscriptionPlans.Commands.CreateCommand;
using Pos.tenant.Application.Features.SubscriptionPlans.Commands.DeleteCommand;
using Pos.tenant.Application.Features.SubscriptionPlans.Commands.UpdateCommand;
using Pos.tenant.Application.Features.SubscriptionPlans.DTOs;
using Pos.tenant.Application.Features.SubscriptionPlans.Queries.GetAllActivePlansQuery;
using Pos.tenant.Application.Features.SubscriptionPlans.Queries.GetAllQuery;
using Pos.tenant.Application.Features.SubscriptionPlans.Queries.GetByCodeQuery;
using Pos.tenant.Application.Features.SubscriptionPlans.Queries.GetByIdQuery;
using Pos.tenant.Application.Wrappers;

namespace Pos.tenant.WebApi.Controllers.V1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class SubscriptionPlansController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SubscriptionPlansController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "PlatformAdmins")]
        [HttpGet]
        public async Task<ActionResult<Response<IEnumerable<SubscriptionPlanDto>>>> GetAll()
        {
            var plans = await _mediator.Send(new GetAllSubscriptionPlansQuery());

            return Ok(new Response<IEnumerable<SubscriptionPlanDto>>(
                plans,
                "Subscription plans retrieved successfully."
            ));
        }

        [AllowAnonymous]
        [HttpGet("active")]
        public async Task<ActionResult<Response<IEnumerable<SubscriptionPlanDto>>>> GetActive()
        {
            var plans = await _mediator.Send(new GetActivePlansQuery());

            return Ok(new Response<IEnumerable<SubscriptionPlanDto>>(
                plans,
                "Active subscription plans retrieved successfully."
            ));
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Response<SubscriptionPlanDto>>> GetById(Guid id)
        {
            var plan = await _mediator.Send(new GetSubscriptionPlanByIdQuery { Id=id});
            if (plan == null)
            {
                return NotFound(new Response<SubscriptionPlanDto>
                {
                    Succeeded = false,
                    Message = "Subscription plan not found."
                });
            }

            return Ok(new Response<SubscriptionPlanDto>(
                plan,
                "Subscription plan retrieved successfully."
            ));
        }

        [AllowAnonymous]
        [HttpGet("by-code/{code}")]
        public async Task<ActionResult<Response<SubscriptionPlanDto>>> GetByCode(string code)
        {
            var plan = await _mediator.Send(new GetPlanByCodeQuery
            {
                Code = code
            });

            if (plan == null)
            {
                return NotFound(new Response<SubscriptionPlanDto>
                {
                    Succeeded = false,
                    Message = "Subscription plan not found."
                });
            }

            return Ok(new Response<SubscriptionPlanDto>(
                plan,
                "Subscription plan retrieved successfully."
            ));
        }

        [Authorize(Policy = "PlatformAdmins")]
        [HttpPost]
        public async Task<ActionResult<Response<Guid>>> Post([FromBody] CreateSubscriptionPlanCommand subscriptionPlan)
        {
            var result = await _mediator.Send(subscriptionPlan);

            if (result.IsFailure)
            {
                return BadRequest(new Response<Guid>
                {
                    Succeeded = false,
                    Message = "Subscription plan creation failed.",
                    Errors = result.Errors
                });
            }


            return Ok(new Response<Guid>(
                result.Value,
                "Subscription plan created successfully."
            ));
        }
        [Authorize(Policy = "PlatformAdmins")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<Response<Guid>>> Update(Guid id,[FromBody] UpdateSubscriptionPlanCommand command)
        {
            command.Id = id;

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
                {
                    return NotFound(new Response<Guid>
                    {
                        Succeeded = false,
                        Message = "Subscription plan not found.",
                        Errors = result.Errors
                    });
                }

                return BadRequest(new Response<Guid>
                {
                    Succeeded = false,
                    Message = "Subscription plan update failed.",
                    Errors = result.Errors
                });
            }

            return Ok(new Response<Guid>(
                result.Value,
                "Subscription plan updated successfully."
            ));
        }
        [Authorize(Policy = "PlatformAdmins")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<Response<Guid>>> Delete(Guid id)
        {
            var result = await _mediator.Send(new DeleteSubscriptionPlanCommand
            {
                Id = id
            });

            if (result.IsFailure)
            {
                if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
                {
                    return NotFound(new Response<Guid>
                    {
                        Succeeded = false,
                        Message = "Subscription plan not found.",
                        Errors = result.Errors
                    });
                }

                return BadRequest(new Response<Guid>
                {
                    Succeeded = false,
                    Message = "Subscription plan deletion failed.",
                    Errors = result.Errors
                });
            }

            return Ok(new Response<Guid>(
                result.Value,
                "Subscription plan deleted successfully."
            ));
        }
    }
}