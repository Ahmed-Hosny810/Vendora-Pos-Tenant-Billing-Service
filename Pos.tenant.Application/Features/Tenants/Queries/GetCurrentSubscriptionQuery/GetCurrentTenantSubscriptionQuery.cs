using MediatR;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;

namespace Pos.tenant.Application.Features.Tenants.Queries.GetCurrentSubscriptionQuery;

public class GetCurrentTenantSubscriptionQuery : IRequest<TenantSubscriptionDto>
{
}

public class GetCurrentTenantSubscriptionQueryHandler
    : IRequestHandler<GetCurrentTenantSubscriptionQuery, TenantSubscriptionDto>
{
    private readonly ITenantSubscriptionRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentTenantSubscriptionQueryHandler(
        ITenantSubscriptionRepositoryAsync repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<TenantSubscriptionDto> Handle(
        GetCurrentTenantSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var subscription = await _repository.GetCurrentPlanByTenantIdAsync(tenantId.Value, cancellationToken);
        if (subscription == null)
            throw new KeyNotFoundException("Tenant subscription was not found.");

        return new TenantSubscriptionDto(subscription);
    }
}
