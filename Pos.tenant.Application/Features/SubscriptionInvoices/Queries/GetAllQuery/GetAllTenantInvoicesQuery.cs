using AutoMapper;
using MediatR;
using Pos.tenant.Application.Features.SubscriptionInvoices.DTOS;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.SubscriptionInvoices.Queries.GetAllQuery
{
    public class GetAllTenantInvoicesQuery: IRequest<PagedResponse<IEnumerable<SubscriptionInvoiceDto>>>
    {
        public GetAllTenantInvoicesQueryParameter Parameter { get; set; } = new();
    }
    public class GetAllTenantInvoicesQueryHandler : IRequestHandler<GetAllTenantInvoicesQuery, PagedResponse<IEnumerable<SubscriptionInvoiceDto>>>
    {
        private readonly ISubscriptionInvoiceRepositoryAsync _subscriptionInvoiceRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public GetAllTenantInvoicesQueryHandler(ISubscriptionInvoiceRepositoryAsync subscriptionInvoiceRepository,IMapper mapper, ICurrentUserService currentUser)
        {
            _subscriptionInvoiceRepository = subscriptionInvoiceRepository;
            _mapper = mapper;
            _currentUser = currentUser;
        }
        public async Task<PagedResponse<IEnumerable<SubscriptionInvoiceDto>>> Handle(GetAllTenantInvoicesQuery request, CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            var result = await _subscriptionInvoiceRepository.GetInvoicesPagedResponseAsync(tenantId.Value, request.Parameter.Filter, request.Parameter.OrderKey,
                                         request.Parameter.OrderDescending,request.Parameter.PageNumber, request.Parameter.PageSize);


            var invoicesDtos = _mapper.Map<IEnumerable<SubscriptionInvoiceDto>>(result.Data);

            return new PagedResponse<IEnumerable<SubscriptionInvoiceDto>>(invoicesDtos, result.PageNumber, result.PageSize, result.TotalCount);
        }
    }
}
