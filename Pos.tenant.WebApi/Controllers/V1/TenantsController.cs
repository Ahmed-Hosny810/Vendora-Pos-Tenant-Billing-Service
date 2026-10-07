using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos.tenant.Application.Features.SubscriptionPlans.Commands.CreateCommand;
using Pos.tenant.Application.Features.Tenants.Commands.CreateCommand;
using Pos.tenant.Application.Features.Tenants.Commands.Settings;
using Pos.tenant.Application.Features.Tenants.Commands.StatusCommand;
using Pos.tenant.Application.Features.Tenants.Commands.TenantUsageCountersCommands;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Features.Tenants.Queries.GetAllQuery;
using Pos.tenant.Application.Features.Tenants.Queries.GetByIdQuery;
using Pos.tenant.Application.Features.Tenants.Queries.GetCurrentQuery;
using Pos.tenant.Application.Features.Tenants.Queries.GetStatusHistoryQuery;
using Pos.tenant.Application.Features.Tenants.Queries.GetTenantSettingsQuery;
using Pos.tenant.Application.Features.Tenants.Queries.GetTenantUsageCounters;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Enums;

namespace Pos.tenant.WebApi.Controllers.V1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TenantsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TenantsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [Authorize(Policy = "TenantUserOnly")]
        [HttpGet("me")]
        public async Task<ActionResult<Response<TenantDto>>> GetCurrent(
            [FromQuery] TenantIncludes includes, CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new GetCurrentTenantQuery { Includes = includes }, cancellationToken));
        }
        [Authorize(Policy = "CanCreateTenantDuringOnboarding")]
        [HttpPost]
        public async Task<ActionResult<Response<Guid>>> Post([FromBody] CreateTenantCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                return BadRequest(new Response<Guid>
                {
                    Succeeded = false,
                    Message = "Tenant creation failed.",
                    Errors = result.Errors
                });
            }


            return Ok(new Response<Guid>(
                result.Value,
                "Tenant created successfully"
            ));
        }
        [Authorize(Policy ="PlatformAdmins")]
        [HttpPost("GetAll")]
        public async Task<ActionResult<PagedResponse<IEnumerable<TenantDto>>>> Get([FromBody] GetAllTenantsQueryParameter parameter)
        {
            return Ok(await _mediator.Send(new GetAllTenantsQuery { Parameter = parameter }));
        }
        
        [Authorize(Policy = "PlatformAdmins")]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Response<TenantDto>>> GetById(Guid id, [FromQuery] TenantIncludes includes)
        {
            return Ok(await _mediator.Send(new GetTenantByIdQuery { TenantId = id, Includes = includes }));
        }
        [Authorize(Policy = "PlatformAdmins")]
        [HttpPost("{id:guid}/activate")]
        public async Task<ActionResult<Response<Guid>>> Activate( Guid id,[FromBody] ActivateTenantCommand command)
        {
            var result = await _mediator.Send(new ActivateTenantCommand
            {
                TenantId = id,
                Reason = command.Reason
            });

            if (result.IsFailure)
            {
                return BadRequest(new Response<Guid>
                {
                    Succeeded = false,
                    Message = "Tenant activation failed.",
                    Errors = result.Errors
                });
            }

            return Ok(new Response<Guid>(
                result.Value,
                "Tenant activated successfully."
            ));
        }
        [Authorize(Policy = "PlatformAdmins")]
        [HttpPost("{id:guid}/suspend")]
        public async Task<ActionResult<Response<Guid>>> Suspend(
            Guid id,
            [FromBody] SuspendTenantCommand command)
        {
            var result = await _mediator.Send(new SuspendTenantCommand
            {
                TenantId = id,
                Reason = command.Reason
            });

            if (result.IsFailure)
            {
                return BadRequest(new Response<Guid>
                {
                    Succeeded = false,
                    Message = "Tenant suspension failed.",
                    Errors = result.Errors
                });
            }

            return Ok(new Response<Guid>(
                result.Value,
                "Tenant suspended successfully."
            ));
        }
        [Authorize(Policy = "PlatformAdmins")]
        [HttpPost("{id:guid}/cancel")]
        public async Task<ActionResult<Response<Guid>>> Cancel(Guid id,[FromBody] CancelTenantCommand command)
        {
            var result = await _mediator.Send(new CancelTenantCommand
            {
                TenantId = id,
                Reason = command.Reason
            });

            if (result.IsFailure)
            {
                return BadRequest(new Response<Guid>
                {
                    Succeeded = false,
                    Message = "Tenant cancellation failed.",
                    Errors = result.Errors
                });
            }

            return Ok(new Response<Guid>(
                result.Value,
                "Tenant cancelled successfully."
            ));
        }

        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpGet("status-history")]
        public async Task<ActionResult<Response<IEnumerable<TenantStatusHistoryDto>>>> GetStatusHistory(CancellationToken cancellationToken)
        {
            var history = await _mediator.Send(new GetTenantStatusHistoryQuery(), cancellationToken);

            return Ok(new Response<IEnumerable<TenantStatusHistoryDto>>(
                history,
                "Tenant status history retrieved successfully."
            ));
        }

        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpPut("settings")]
        public async Task<ActionResult<Response<Guid>>> UpdateSettings(
            [FromBody] UpdateTenantSettingsCommand command, CancellationToken cancellationToken)
        {
            var reult = await _mediator.Send(command, cancellationToken);

            if (reult.IsFailure)
            {
                return BadRequest(new Response<Guid>
                {
                    Succeeded = false,
                    Message = "Tenant settings update failed.",
                    Errors = reult.Errors
                });
            }

            return Ok(new Response<Guid>(reult.Value, "Tenant settings updated successfully."));
        }

        [Authorize(Policy = "TenantUserOnly")]
        [HttpGet("settings")]
        public async Task<ActionResult<Response<TenantSettingsDto>>> GetTenantSettings(CancellationToken cancellationToken)
        {
            var history = await _mediator.Send(new GetTenantSettingsQuery(), cancellationToken);

            return Ok(new Response<TenantSettingsDto>(
                history,
                "Tenant settings retrieved successfully."
            ));
        }


        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpGet("usage")]
        public async Task<ActionResult<Response<TenantUsageCountersDto>>> GetUsage(CancellationToken cancellationToken)
        {
            var usage = await _mediator.Send(new GetTenantUsageQuery(), cancellationToken);

            return Ok(new Response<TenantUsageCountersDto>(
                usage,
                "Tenant usage counters retrieved successfully."
            ));
        }
        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpPost("usage/increase-branch")]
        public async Task<ActionResult<Response<TenantUsageUpdateResult>>> IncreaseBranch(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new IncreaseTenantUsageCommand
            {
                CounterType = TenantUsageCounterType.Branch
            }, cancellationToken);

            return HandleUsageCommandResult(result, "Branch usage increased successfully.");
        }
        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpPost("usage/increase-product")]
        public async Task<ActionResult<Response<TenantUsageUpdateResult>>> IncreaseProduct(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new IncreaseTenantUsageCommand
            {
                CounterType = TenantUsageCounterType.Product
            }, cancellationToken);

            return HandleUsageCommandResult(result, "Product usage increased successfully.");
        }
        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpPost("usage/increase-cashier")]
        public async Task<ActionResult<Response<TenantUsageUpdateResult>>> IncreaseCashier(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new IncreaseTenantUsageCommand
            {
                CounterType = TenantUsageCounterType.Cashier
            }, cancellationToken);

            return HandleUsageCommandResult(result, "Cashier usage increased successfully.");
        }
        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpPost("usage/decrease-branch")]
        public async Task<ActionResult<Response<TenantUsageUpdateResult>>> DecreaseBranch(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DecreaseTenantUsageCommand
            {
                CounterType = TenantUsageCounterType.Branch
            }, cancellationToken);

            return HandleUsageCommandResult(result, "Branch usage decreased successfully.");
        }
        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpPost("usage/decrease-product")]
        public async Task<ActionResult<Response<TenantUsageUpdateResult>>> DecreaseProduct(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DecreaseTenantUsageCommand
            {
                CounterType = TenantUsageCounterType.Product
            }, cancellationToken);

            return HandleUsageCommandResult(result, "Product usage decreased successfully.");
        }
        [Authorize(Policy = "TenantOwnerOnly")]
        [HttpPost("usage/decrease-cashier")]
        public async Task<ActionResult<Response<TenantUsageUpdateResult>>> DecreaseCashier(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DecreaseTenantUsageCommand
            {
                CounterType = TenantUsageCounterType.Cashier
            }, cancellationToken);

            return HandleUsageCommandResult(result, "Cashier usage decreased successfully.");
        }

        private ActionResult<Response<TenantUsageUpdateResult>> HandleUsageCommandResult(Result<TenantUsageUpdateResult> result,string successMessage)
        {
            if (result.IsFailure)
            {
                return BadRequest(new Response<TenantUsageUpdateResult>
                {
                    Succeeded = false,
                    Message = "Tenant usage operation failed.",
                    Errors = result.Errors
                });
            }

            return Ok(new Response<TenantUsageUpdateResult>(
                result.Value,
                successMessage
            ));
        }
    }
}   
