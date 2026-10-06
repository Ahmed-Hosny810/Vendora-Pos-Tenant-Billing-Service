using AutoMapper;
using MediatR;
using Pos.tenant.Application.Exceptions;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.Tenants.Queries.GetTenantUsageCounters
{
    public class GetTenantUsageQuery:IRequest<TenantUsageCountersDto>
    {
    }
    public class GetTenantUsageQueryHandler : IRequestHandler<GetTenantUsageQuery, TenantUsageCountersDto>
    {
        private readonly ITenantUsageCountersRepositoryAsync _tenantUsageCountersRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetTenantUsageQueryHandler(ITenantUsageCountersRepositoryAsync tenantUsageCountersRepository,IMapper mapper, ICurrentUserService currentUser)
        {
            _tenantUsageCountersRepository = tenantUsageCountersRepository;
            _mapper = mapper;
            _currentUser = currentUser;
        }
        public async Task<TenantUsageCountersDto> Handle(GetTenantUsageQuery request, CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            var tenantUsage=await _tenantUsageCountersRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken);

            if (tenantUsage == null)
                throw new ApiException($"Tenant usage counters with Tenant ID {tenantId.Value} not found.");

            return _mapper.Map<TenantUsageCountersDto>(tenantUsage);
        }
    }
}
