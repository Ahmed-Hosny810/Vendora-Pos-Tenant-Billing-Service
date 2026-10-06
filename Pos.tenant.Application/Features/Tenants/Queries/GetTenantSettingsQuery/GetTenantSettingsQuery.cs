using AutoMapper;
using MediatR;
using Pos.tenant.Application.Exceptions;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;

namespace Pos.tenant.Application.Features.Tenants.Queries.GetTenantSettingsQuery
{
    public class GetTenantSettingsQuery:IRequest<TenantSettingsDto>
    {
    }
    public class GetTenantSettingsQueryHandler : IRequestHandler<GetTenantSettingsQuery, TenantSettingsDto>
    {
        private readonly ITenantSettingsRepositoryAsync _tenantSettingsRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetTenantSettingsQueryHandler(ITenantSettingsRepositoryAsync tenantSettingsRepository,IMapper mapper, ICurrentUserService currentUser)
        {
            _tenantSettingsRepository = tenantSettingsRepository;
            _mapper = mapper;
            _currentUser = currentUser;
        }
        public async Task<TenantSettingsDto> Handle(GetTenantSettingsQuery request, CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            var tenantSettings = await _tenantSettingsRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken);

            if (tenantSettings == null) 
                throw new ApiException ($"Tenant settings not found for TenantId: {tenantId.Value}");

            return _mapper.Map<TenantSettingsDto>(tenantSettings);

        }
    }
}
