using AutoMapper;
using MediatR;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.Tenants.Commands.CreateCommand
{
    public class CreateTenantCommand:IRequest<Result<Guid>>
    {
        public string? NameAr { get; set; }
        public string NameEn { get; set; } = null!;
        public string BusinessTypeCode { get; set; } = null!;
        public string CurrencyCode { get; set; } = "EGP";
        public string InventoryMode { get; set; } = TenantInventoryModes.TrackStock;
        public string PlanCode { get; set; }= null!;
    }
    public class CreateTenantCommandHandler: IRequestHandler<CreateTenantCommand, Result<Guid>>
    {
        private readonly ITenantRepositoryAsync _tenantRepository;
        private readonly ISubscriptionPlanRepositoryAsync _subscriptionPlanRepository;
        private readonly ITenantSettingsRepositoryAsync _tenantSettingsRepository;
        private readonly ITenantUsageCountersRepositoryAsync _tenantUsageCountersRepository;
        private readonly ITenantSubscriptionRepositoryAsync _tenantSubscriptionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTenantCommandHandler(ITenantRepositoryAsync tenantRepository,ISubscriptionPlanRepositoryAsync subscriptionPlanRepository,
            ITenantSettingsRepositoryAsync tenantSettingsRepository, ITenantUsageCountersRepositoryAsync tenantUsageCountersRepository,
            ITenantSubscriptionRepositoryAsync tenantSubscriptionRepository,IUnitOfWork unitOfWork)
        {
            _tenantRepository = tenantRepository;
            _subscriptionPlanRepository = subscriptionPlanRepository;
            _tenantSettingsRepository = tenantSettingsRepository;
            _tenantUsageCountersRepository = tenantUsageCountersRepository;
            _tenantSubscriptionRepository = tenantSubscriptionRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<Guid>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            var nameEn = request.NameEn.Trim();
            var nameAr = request.NameAr?.Trim();

            var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
            var businessTypeCode = request.BusinessTypeCode.Trim().ToUpperInvariant();
            var planCode = request.PlanCode.Trim().ToUpperInvariant();

            //Find SubscriptionPlan

            var subscriptionPlan = await _subscriptionPlanRepository.GetByCodeAsync(planCode);
            if (subscriptionPlan==null)
                 return Result<Guid>.Failure("Invalid Subscription Plan.");

            if (!subscriptionPlan.IsActive)
                return Result<Guid>.Failure("The selected subscription plan is inactive.");

            var tenantId = Guid.NewGuid();

            var tenant = new Tenant
            {
                Id = tenantId,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                BusinessTypeCode = businessTypeCode,
                CurrencyCode = currencyCode,
                InventoryMode = request.InventoryMode,
                Status = TenantStatuses.Pending
            };

            var tenantSettings= new TenantSettings
            {
                TenantId = tenant.Id,
                
            };
            var tenantSubscription = new TenantSubscription
            {
                TenantId = tenant.Id,
                PlanId= subscriptionPlan.Id,
                Status= TenantSubscriptionStatuses.Pending,
                CurrentPeriodStart = null,
                CurrentPeriodEnd = null,
                GracePeriodEndsAt = null
            };

            var tenantUsageCounter = new TenantUsageCounters
            {
                TenantId = tenant.Id,
                BranchCount = 0,
                ProductCount = 0,
                CashierCount = 0,
                UpdatedAt = DateTime.UtcNow
            };

            await _tenantRepository.AddAsync(tenant);
            await _tenantSettingsRepository.AddAsync(tenantSettings);
            await _tenantSubscriptionRepository.AddAsync(tenantSubscription);
            await _tenantUsageCountersRepository.AddAsync(tenantUsageCounter);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(tenant.Id);
        }
    }

}
