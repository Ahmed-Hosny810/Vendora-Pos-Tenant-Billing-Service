using MediatR;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.Tenants.Commands.TenantUsageCountersCommands
{
    public class IncreaseTenantUsageCommand : IRequest<Result<TenantUsageUpdateResult>>
    {
        public Guid TenantId { get; set; }
        public TenantUsageCounterType CounterType { get; set; }
    }

    public class IncreaseTenantUsageCommandHandler : IRequestHandler<IncreaseTenantUsageCommand, Result<TenantUsageUpdateResult>>
    {
        private readonly ITenantUsageCountersRepositoryAsync _tenantUsageCountersRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantSubscriptionRepositoryAsync _tenantSubscriptionRepository;

        public IncreaseTenantUsageCommandHandler(ITenantUsageCountersRepositoryAsync tenantUsageCountersRepository,IUnitOfWork unitOfWork,
            ITenantSubscriptionRepositoryAsync tenantSubscriptionRepository)
        {
            _tenantUsageCountersRepository = tenantUsageCountersRepository;
            _unitOfWork = unitOfWork;
            _tenantSubscriptionRepository = tenantSubscriptionRepository;
        }
        public async Task<Result<TenantUsageUpdateResult>> Handle(IncreaseTenantUsageCommand request, CancellationToken cancellationToken)
        {
            var tenantUsageCounter = await _tenantUsageCountersRepository.GetByIdAsync(request.TenantId);

            if (tenantUsageCounter == null)
            {
                return Result<TenantUsageUpdateResult>.Failure($"Tenant usage counters with Tenant ID {request.TenantId} not found.");
            }

            var subscription = await _tenantSubscriptionRepository
                .GetCurrentPlanByTenantIdAsync(request.TenantId);

            if (subscription == null)
            {
                return Result<TenantUsageUpdateResult>.Failure($"Tenant subscription with Tenant ID {request.TenantId} not found.");
            }

            if (subscription.Status != TenantSubscriptionStatuses.Active)
                return Result<TenantUsageUpdateResult>.Failure("Subscription is not active.");

            if (subscription.Plan == null)
            {
                return Result<TenantUsageUpdateResult>.Failure("Subscription plan not found.");
            }

            var plan = subscription.Plan;

            int usedCount;
            int? maxAllowed;

            switch (request.CounterType)
            {
                case TenantUsageCounterType.Branch:

                    usedCount = tenantUsageCounter.BranchCount;
                    maxAllowed = plan.BranchLimit;

                    if (maxAllowed.HasValue && usedCount >= maxAllowed.Value)
                    { 
                        return Result<TenantUsageUpdateResult>.Failure($"Branch limit exceeded. Current limit is {maxAllowed.Value}.");
                    }
                    tenantUsageCounter.BranchCount++;
                    usedCount = tenantUsageCounter.BranchCount;
                    break;
                case TenantUsageCounterType.Product:

                    usedCount = tenantUsageCounter.ProductCount;
                    maxAllowed = plan.ProductLimit;

                    if (maxAllowed.HasValue && usedCount >= maxAllowed.Value)
                    {
                        return Result<TenantUsageUpdateResult>.Failure(
                            $"Product limit exceeded. Current limit is {maxAllowed.Value}.");
                    }

                    tenantUsageCounter.ProductCount++;
                    usedCount = tenantUsageCounter.ProductCount;
                    break;

                case TenantUsageCounterType.Cashier:

                    usedCount = tenantUsageCounter.CashierCount;
                    maxAllowed = plan.CashierLimit;

                    if (maxAllowed.HasValue && usedCount >= maxAllowed.Value)
                    {
                        return Result<TenantUsageUpdateResult>.Failure(
                            $"Cashier limit exceeded. Current limit is {maxAllowed.Value}.");
                    }

                    tenantUsageCounter.CashierCount++;
                    usedCount = tenantUsageCounter.CashierCount;
                    break;

                default:
                    return Result<TenantUsageUpdateResult>.Failure($"Invalid counter type: {request.CounterType}");
            }

            tenantUsageCounter.UpdatedAt = DateTime.UtcNow;

            _tenantUsageCountersRepository.Update(tenantUsageCounter);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

             return Result<TenantUsageUpdateResult>.Success(new TenantUsageUpdateResult
             {
                 TenantId = tenantUsageCounter.TenantId,
                 CounterType = request.CounterType,
                 UsedCount = usedCount,
                 MaxAllowed = maxAllowed ?? 0
             });
        }
    }
}
