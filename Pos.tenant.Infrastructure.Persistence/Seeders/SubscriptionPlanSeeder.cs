using Microsoft.EntityFrameworkCore;
using Pos.tenant.Domain.Models;
using Pos.tenant.Infrastructure.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Infrastructure.Persistence.Seeders
{
    public static class SubscriptionPlanSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            var plans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan
                {
                    Code = "STANDARD",
                    NameEn = "Standard",
                    NameAr = "القياسية",
                    DescriptionEn = "Basic plan for small stores starting with one branch.",
                    DescriptionAr = "خطة أساسية للمتاجر الصغيرة التي تبدأ بفرع واحد.",
                    MonthlyPrice = 4500,
                    CurrencyCode="EGP",
                    BranchLimit = 1,
                    ProductLimit = 500,
                    CashierLimit = 3,
                    AllowVariants = true,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Code = "PLUS",
                    NameEn = "Plus",
                    NameAr = "بلس",
                    DescriptionEn = "Recommended plan for growing stores with multiple users and branches.",
                    DescriptionAr = "خطة مناسبة للمتاجر المتنامية التي تحتاج أكثر من فرع ومستخدم.",
                    MonthlyPrice = 7500,
                    CurrencyCode="EGP",
                    BranchLimit = 3,
                    ProductLimit = 1500,
                    CashierLimit = 10,
                    AllowVariants = true,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Code = "PREMIUM",
                    NameEn = "Premium",
                    NameAr = "المميزة",
                    DescriptionEn = "Advanced plan for larger businesses with unlimited usage limits.",
                    DescriptionAr = "خطة متقدمة للشركات الأكبر مع حدود استخدام غير محدودة.",
                    MonthlyPrice = 15000,
                    CurrencyCode="EGP",
                    BranchLimit = null,
                    ProductLimit = null,
                    CashierLimit = null,
                    AllowVariants = true,
                    IsActive = true
                }
            };

            var planCodes = plans.Select(p => p.Code).ToList();

            var existingCodes = await context.SubscriptionPlans
                .Where(p => planCodes.Contains(p.Code))
                .Select(p => p.Code)
                .ToListAsync();

            var existingCodesSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingPlans = plans
                .Where(p => !existingCodesSet.Contains(p.Code))
                .ToList();

            if (missingPlans.Count == 0)
                return;

            await context.SubscriptionPlans.AddRangeAsync(missingPlans);
            await context.SaveChangesAsync();
        }
    }
}
