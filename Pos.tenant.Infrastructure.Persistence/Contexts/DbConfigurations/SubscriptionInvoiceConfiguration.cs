using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Infrastructure.Persistence.Contexts.DbConfigurations
{
    public class SubscriptionInvoiceConfiguration : IEntityTypeConfiguration<SubscriptionInvoice>
    {
        public void Configure(EntityTypeBuilder<SubscriptionInvoice> builder)
        {
            builder.ToTable("SubscriptionInvoices");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.InvoiceNumber)
                .HasMaxLength(80)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.PeriodStart)
                .IsRequired();

            builder.Property(x => x.PeriodEnd)
                .IsRequired();

            builder.Property(x => x.Total)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue(InvoiceStatuses.Unpaid)
                .IsRequired();

            builder.Property(x => x.DueDate)
                .IsRequired();

            builder.Property(x => x.RowVersion)
                .IsRowVersion();

            builder.Property(x => x.PaidAt)
                .IsRequired(false);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.HasIndex(x => new { x.TenantId, x.InvoiceNumber })
                    .IsUnique();

            builder.HasIndex(x => new { x.TenantId, x.Status });

            builder.HasIndex(x => new { x.TenantId, x.DueDate });

            builder.HasIndex(x => new { x.TenantId,x.TenantSubscriptionId, x.DueDate })
                    .IsUnique();

            builder.HasOne(x => x.Tenant)
                     .WithMany(x => x.SubscriptionInvoices)
                     .HasForeignKey(x => x.TenantId)
                     .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.TenantSubscription)
                .WithMany(x => x.SubscriptionInvoices)
                .HasForeignKey(x => x.TenantSubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
