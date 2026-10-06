using MediatR;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.Tenants.Commands.Settings
{
    public class UpdateTenantSettingsCommand:IRequest<Result<Guid>>
    {
        public decimal DefaultVatRate { get; set; }
        public bool PricesIncludeTax { get; set; }
        public string? ReceiptFooterAr { get; set; }
        public string? ReceiptFooterEn { get; set; }
        public bool AllowReturns { get; set; }
        public decimal DiscountLimitPercent { get; set; }
        public string DefaultLanguage { get; set; } = null!;
    }

    public class UpdateTenantSettingsCommandHandler : IRequestHandler<UpdateTenantSettingsCommand, Result<Guid>>
    {
        private readonly ITenantSettingsRepositoryAsync _tenantSettingsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateTenantSettingsCommandHandler(ITenantSettingsRepositoryAsync tenantSettingsRepository,IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _tenantSettingsRepository = tenantSettingsRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }
        public async Task<Result<Guid>> Handle(UpdateTenantSettingsCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            var tenantSettings = await _tenantSettingsRepository.GetTenantSettingsQuery(tenantId.Value);

            if (tenantSettings == null)
            {
                return Result<Guid>.Failure("Tenant settings were not found.");
            }

            tenantSettings.DefaultVatRate = request.DefaultVatRate;
            tenantSettings.PricesIncludeTax = request.PricesIncludeTax;
            tenantSettings.ReceiptFooterAr = string.IsNullOrWhiteSpace(request.ReceiptFooterAr) ? null: request.ReceiptFooterAr.Trim();

            tenantSettings.ReceiptFooterEn = string.IsNullOrWhiteSpace(request.ReceiptFooterEn)? null: request.ReceiptFooterEn.Trim();
            tenantSettings.AllowReturns = request.AllowReturns;
            tenantSettings.DiscountLimitPercent = request.DiscountLimitPercent;
            tenantSettings.DefaultLanguage = request.DefaultLanguage.Trim().ToLowerInvariant();

            _tenantSettingsRepository.Update(tenantSettings);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var tenantSettingsDto = new TenantSettingsDto
            {
                DefaultVatRate = tenantSettings.DefaultVatRate,
                PricesIncludeTax = tenantSettings.PricesIncludeTax,
                ReceiptFooterAr = tenantSettings.ReceiptFooterAr,
                ReceiptFooterEn = tenantSettings.ReceiptFooterEn,
                AllowReturns = tenantSettings.AllowReturns,
                DiscountLimitPercent = tenantSettings.DiscountLimitPercent,
                DefaultLanguage = tenantSettings.DefaultLanguage
            };

            return Result<Guid>.Success(tenantSettings.TenantId);
        }
    }
}
