using AutoMapper;
using MediatR;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Features.Tenants.Queries.GetAllQuery;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;

namespace Pos.tenant.Application.Features.Tenants.Queries.GetCurrentQuery;

public class GetCurrentTenantQuery : IRequest<Response<TenantDto>>
{
    public TenantIncludes Includes { get; set; } = new();
}

public class GetCurrentTenantQueryHandler : IRequestHandler<GetCurrentTenantQuery, Response<TenantDto>>
{
    private readonly ITenantRepositoryAsync _tenantRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetCurrentTenantQueryHandler(ITenantRepositoryAsync tenantRepository,
        ICurrentUserService currentUser, IMapper mapper)
    {
        _tenantRepository = tenantRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Response<TenantDto>> Handle(GetCurrentTenantQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var tenant = await _tenantRepository.GetTenantByIdAsync(tenantId.Value, request.Includes);
        if (tenant == null)
            throw new KeyNotFoundException("Tenant was not found.");

        return new Response<TenantDto>(_mapper.Map<TenantDto>(tenant));
    }
}
