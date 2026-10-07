using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Application.Features.Tenants.DTOS
{
    public class TenantSubscriptionDto
    {
        public TenantSubscriptionDto()
        {
        }

        public TenantSubscriptionDto(TenantSubscription subscription)
        {
            Id = subscription.Id;
            TenantId = subscription.TenantId;
            PlanId = subscription.PlanId;

            Status = subscription.Status;

            CurrentPeriodStart = subscription.CurrentPeriodStart;
            CurrentPeriodEnd = subscription.CurrentPeriodEnd;
            GracePeriodEndsAt = subscription.GracePeriodEndsAt;

            if (subscription.Plan != null)
            {
                PlanCode = subscription.Plan.Code;
                PlanNameAr = subscription.Plan.NameAr;
                PlanNameEn = subscription.Plan.NameEn;
                MonthlyPrice = subscription.Plan.MonthlyPrice;
                CurrencyCode = subscription.Plan.CurrencyCode;
                BranchLimit = subscription.Plan.BranchLimit;
                ProductLimit = subscription.Plan.ProductLimit;
                CashierLimit = subscription.Plan.CashierLimit;
                AllowVariants = subscription.Plan.AllowVariants;
            }
        }

        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid PlanId { get; set; }

        public string? PlanCode { get; set; }
        public string? PlanNameAr { get; set; }
        public string? PlanNameEn { get; set; }

        public decimal? MonthlyPrice { get; set; }
        public string? CurrencyCode { get; set; }
        public int? BranchLimit { get; set; }
        public int? ProductLimit { get; set; }
        public int? CashierLimit { get; set; }
        public bool AllowVariants { get; set; }

        public string Status { get; set; } = null!;

        public DateTime? CurrentPeriodStart { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }
        public DateTime? GracePeriodEndsAt { get; set; }
    }
}
