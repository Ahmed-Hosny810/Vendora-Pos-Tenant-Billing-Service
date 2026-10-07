using AutoMapper;
using MediatR;
using Pos.tenant.Application.Features.SubscriptionPlans.DTOs;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.SubscriptionPlans.Queries.GetByIdQuery
{
    public class GetSubscriptionPlanByIdQuery : IRequest<SubscriptionPlanDto?>
    {
        public Guid Id { get; set; }
    }

    public class GetSubscriptionPlanByIdQueryHandler
        : IRequestHandler<GetSubscriptionPlanByIdQuery, SubscriptionPlanDto?>
    {
        private readonly ISubscriptionPlanRepositoryAsync _subscriptionPlanRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetSubscriptionPlanByIdQueryHandler(
            ISubscriptionPlanRepositoryAsync subscriptionPlanRepository,
            IMapper mapper, ICurrentUserService currentUser)
        {
            _subscriptionPlanRepository = subscriptionPlanRepository;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<SubscriptionPlanDto?> Handle(
            GetSubscriptionPlanByIdQuery request,
            CancellationToken cancellationToken)
        {
            var subscriptionPlan = await _subscriptionPlanRepository.GetByIdAsync(request.Id);

            if (subscriptionPlan == null)
                return null;

            // Match the PlatformAdmins policy; tenant roles cannot reveal inactive plans.
            var isPlatformAdmin = _currentUser.UserType == "Platform" &&
                _currentUser.Roles.Contains("TenantAdmin");

            if (!subscriptionPlan.IsActive && !isPlatformAdmin)
                return null;

            return _mapper.Map<SubscriptionPlanDto>(subscriptionPlan);
        }
    }
}
