using AutoMapper;
using MediatR;
using Pos.tenant.Application.Exceptions;
using Pos.tenant.Application.Features.SubscriptionInvoices.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.SubscriptionInvoices.Queries.GetByIdQuery
{
    public class GetInvoiceByIdQuery:IRequest<SubscriptionInvoiceDto>
    {
        public Guid InvoiceId { get; set; }
    }

    public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, SubscriptionInvoiceDto>
    {
        private readonly ISubscriptionInvoiceRepositoryAsync _subscriptionInvoiceRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetInvoiceByIdQueryHandler(ISubscriptionInvoiceRepositoryAsync subscriptionInvoiceRepository, IMapper mapper, ICurrentUserService currentUser)
        {
            _subscriptionInvoiceRepository = subscriptionInvoiceRepository;
            _mapper = mapper;
            _currentUser = currentUser;
        }
        public async Task<SubscriptionInvoiceDto> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            var invoice = await _subscriptionInvoiceRepository.GetByTenantAndIdAsync(tenantId.Value, request.InvoiceId, cancellationToken);

            if (invoice == null)
                throw new KeyNotFoundException("Invoice not found.");

            return _mapper.Map<SubscriptionInvoiceDto>(invoice);
        }
    }
}
