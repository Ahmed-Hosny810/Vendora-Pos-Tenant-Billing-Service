using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.IntegrationTests
{
    public static class TestDataFactory
    {
        public static Tenant CreateTenant()
        {
            return new Tenant
            {
                Id = Guid.NewGuid(),
                NameAr = "مطعم اختبار",
                NameEn = "Test Restaurant",
                BusinessTypeCode = "RESTAURANT",
                Status = TenantStatuses.Pending,
                CurrencyCode = "EGP",
                InventoryMode = "TrackStock"
            };
        }

        public static TenantSubscription CreateSubscription(Guid tenantId)
        {
            return new TenantSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Status = TenantSubscriptionStatuses.Pending,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
            };
        }

        public static SubscriptionInvoice CreateInvoice(Guid tenantId)
        {
            var now = DateTime.UtcNow;

            return new SubscriptionInvoice
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
                PeriodStart = now,
                PeriodEnd = now.AddMonths(1),
                DueDate = now.AddDays(7),
                Total = 500,
                Status = InvoiceStatuses.Unpaid
            };
        }
    }
}
