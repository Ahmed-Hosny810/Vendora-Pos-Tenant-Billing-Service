using AutoMapper;
using MediatR;
using Pos.tenant.Application.Features.SubscriptionPayments.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;

namespace Pos.tenant.Application.Features.SubscriptionPayments.Queries.GetByIdQuery;

public class GetSubscriptionPaymentByIdQuery : IRequest<SubscriptionPaymentDto>
{
    public Guid PaymentId { get; set; }
}

public class GetSubscriptionPaymentByIdQueryHandler
    : IRequestHandler<GetSubscriptionPaymentByIdQuery, SubscriptionPaymentDto>
{
    private readonly ISubscriptionPaymentRepositoryAsync _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetSubscriptionPaymentByIdQueryHandler(ISubscriptionPaymentRepositoryAsync repository,
        ICurrentUserService currentUser, IMapper mapper)
    {
        _repository = repository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<SubscriptionPaymentDto> Handle(
        GetSubscriptionPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new UnauthorizedAccessException("A valid tenant is required.");

        var payment = await _repository.GetByTenantAndIdAsync(tenantId.Value, request.PaymentId, cancellationToken);
        if (payment == null)
            throw new KeyNotFoundException("Subscription payment was not found.");

        return _mapper.Map<SubscriptionPaymentDto>(payment);
    }
}
