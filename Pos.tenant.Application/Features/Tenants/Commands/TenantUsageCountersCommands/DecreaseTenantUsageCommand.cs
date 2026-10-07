using MediatR;
using Pos.tenant.Application.Features.Tenants.DTOS;
using Pos.tenant.Application.Interfaces.Repositories;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Application.Wrappers;
using Pos.tenant.Domain.Enums;

namespace Pos.tenant.Application.Features.Tenants.Commands.TenantUsageCountersCommands
{
    public class DecreaseTenantUsageCommand : IRequest<Result<TenantUsageUpdateResult>>
    {

        public TenantUsageCounterType CounterType { get; set; }
    }

    public class DecreaseTenantUsageCommandHandler
        : IRequestHandler<DecreaseTenantUsageCommand, Result<TenantUsageUpdateResult>>
    {
        private readonly ITenantUsageCountersRepositoryAsync _tenantUsageCountersRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public DecreaseTenantUsageCommandHandler(
            ITenantUsageCountersRepositoryAsync tenantUsageCountersRepository,
            IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        {
            _tenantUsageCountersRepository = tenantUsageCountersRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<TenantUsageUpdateResult>> Handle(
            DecreaseTenantUsageCommand request,
            CancellationToken cancellationToken)
        {
            var tenantId = _currentUser.TenantId;

            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
                throw new UnauthorizedAccessException("A valid tenant is required.");

            var tenantUsageCounter = await _tenantUsageCountersRepository
                .GetByTenantIdAsync(tenantId.Value, cancellationToken);

            if (tenantUsageCounter == null)
            {
                return Result<TenantUsageUpdateResult>.Failure(
                    $"Tenant usage counters with Tenant ID {tenantId.Value} not found.");
            }

            int usedCount;

            switch (request.CounterType)
            {
                case TenantUsageCounterType.Branch:
                    if (tenantUsageCounter.BranchCount <= 0)
                    {
                        return Result<TenantUsageUpdateResult>.Failure(
                            "Branch count cannot be less than zero.");
                    }

                    tenantUsageCounter.BranchCount--;
                    usedCount = tenantUsageCounter.BranchCount;
                    break;

                case TenantUsageCounterType.Product:
                    if (tenantUsageCounter.ProductCount <= 0)
                    {
                        return Result<TenantUsageUpdateResult>.Failure(
                            "Product count cannot be less than zero.");
                    }

                    tenantUsageCounter.ProductCount--;
                    usedCount = tenantUsageCounter.ProductCount;
                    break;

                case TenantUsageCounterType.Cashier:
                    if (tenantUsageCounter.CashierCount <= 0)
                    {
                        return Result<TenantUsageUpdateResult>.Failure(
                            "Cashier count cannot be less than zero.");
                    }

                    tenantUsageCounter.CashierCount--;
                    usedCount = tenantUsageCounter.CashierCount;
                    break;

                default:
                    return Result<TenantUsageUpdateResult>.Failure(
                        $"Invalid counter type: {request.CounterType}");
            }

            tenantUsageCounter.UpdatedAt = DateTime.UtcNow;

            _tenantUsageCountersRepository.Update(tenantUsageCounter);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<TenantUsageUpdateResult>.Success(
                new TenantUsageUpdateResult
                {
                    TenantId = tenantUsageCounter.TenantId,
                    CounterType = request.CounterType,
                    UsedCount = usedCount
                });
        }
    }
}