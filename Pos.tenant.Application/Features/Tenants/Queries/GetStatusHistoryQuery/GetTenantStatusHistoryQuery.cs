using AutoMapper;
using MediatR;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;


namespace Pos.tenant.Application.Features.Tenants.Queries.GetStatusHistoryQuery
{
    public class GetTenantStatusHistoryQuery : IRequest<IEnumerable<TenantStatusHistoryDto>>
    {
    }
    public class GetTenantStatusHistoryQueryHandler:IRequestHandler<GetTenantStatusHistoryQuery, IEnumerable<TenantStatusHistoryDto>>
    {
        private readonly ITenantRepositoryAsync _tenantRepository;
        private readonly ITenantStatusHistoryRepositoryAsync _tenantStatusHistoryRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetTenantStatusHistoryQueryHandler(ITenantRepositoryAsync tenantRepository,ITenantStatusHistoryRepositoryAsync tenantStatusHistoryRepository, IMapper mapper, ICurrentUserService currentUser)
        {
            _tenantRepository = tenantRepository;
            _tenantStatusHistoryRepository = tenantStatusHistoryRepository;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<TenantStatusHistoryDto>> Handle( GetTenantStatusHistoryQuery request,CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value);

            if (tenant == null)
                return Enumerable.Empty<TenantStatusHistoryDto>();

            var tenantStatusHistory = await _tenantStatusHistoryRepository.GetByTenantIdAsync(tenantId.Value);

            var tenantStatusHistoryDto=_mapper.Map<IEnumerable<TenantStatusHistoryDto>>(tenantStatusHistory);

            return tenantStatusHistoryDto;
        }
    }
}
