using AutoMapper;
using MediatR;
using Pos.tenant.Application.Features.SubscriptionPayments.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;

namespace Pos.tenant.Application.Features.SubscriptionPayments.Queries.GetAllQuery;

public class GetSubscriptionPaymentsQuery : IRequest<PagedResponse<IEnumerable<SubscriptionPaymentDto>>>
{
    public GetSubscriptionPaymentsQueryParameter Parameter { get; set; } = new();
}

public class GetSubscriptionPaymentsQueryHandler
    : IRequestHandler<GetSubscriptionPaymentsQuery, PagedResponse<IEnumerable<SubscriptionPaymentDto>>>
{
    private readonly ISubscriptionPaymentRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetSubscriptionPaymentsQueryHandler(ISubscriptionPaymentRepositoryAsync repository,
        ICurrentUserService currentUser, IMapper mapper)
    {
        _repository = repository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<PagedResponse<IEnumerable<SubscriptionPaymentDto>>> Handle(
        GetSubscriptionPaymentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var parameter = request.Parameter;
        var payments = await _repository.GetPaymentsPagedResponseAsync(tenantId.Value,
            parameter.Filter, parameter.OrderKey, parameter.OrderDescending,
            parameter.PageNumber, parameter.PageSize, cancellationToken);

        var data = _mapper.Map<IEnumerable<SubscriptionPaymentDto>>(payments.Data);
        return new PagedResponse<IEnumerable<SubscriptionPaymentDto>>(
            data, payments.PageNumber, payments.PageSize, payments.TotalCount);
    }
}
