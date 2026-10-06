using AutoMapper;
using MediatR;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;


namespace Pos.tenant.Application.Features.Tenants.Queries.GetStatusHistoryQuery
{
    public class GetTenantStatusHistoryQuery : IRequest<IEnumerable<TenantStatusHistoryDto>>
    {
        public Guid TenantId { get; set; }
    }
    public class GetTenantStatusHistoryQueryHandler:IRequestHandler<GetTenantStatusHistoryQuery, IEnumerable<TenantStatusHistoryDto>>
    {
        private readonly ITenantRepositoryAsync _tenantRepository;
        private readonly ITenantStatusHistoryRepositoryAsync _tenantStatusHistoryRepository;
        private readonly IMapper _mapper;

        public GetTenantStatusHistoryQueryHandler(ITenantRepositoryAsync tenantRepository,ITenantStatusHistoryRepositoryAsync tenantStatusHistoryRepository, IMapper mapper)
        {
            _tenantRepository = tenantRepository;
            _tenantStatusHistoryRepository = tenantStatusHistoryRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TenantStatusHistoryDto>> Handle( GetTenantStatusHistoryQuery request,CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdAsync(request.TenantId);

            if (tenant == null)
                return Enumerable.Empty<TenantStatusHistoryDto>();

            var tenantStatusHistory = await _tenantStatusHistoryRepository.GetByTenantIdAsync(request.TenantId);

            var tenantStatusHistoryDto=_mapper.Map<IEnumerable<TenantStatusHistoryDto>>(tenantStatusHistory);

            return tenantStatusHistoryDto;
        }
    }
}
