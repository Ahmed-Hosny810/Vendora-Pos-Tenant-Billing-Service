using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.tenant.Domain.Constants;
using Pos.tenant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.tenant.Infrastructure.Persistence.Contexts.DbConfigurations
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenants");

            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.CreatedByUserId)
                .IsUnique()
                .HasFilter("[CreatedByUserId] IS NOT NULL");

            builder.Property(x => x.NameAr)
                .HasMaxLength(200)
                .IsUnicode();

            builder.Property(x => x.NameEn)
                .HasMaxLength(200)
                .IsUnicode();

            builder.Property(x => x.BusinessTypeCode)
                .HasMaxLength(80)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue(TenantStatuses.Pending)
                .IsRequired();

            builder.Property(x => x.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("EGP")
                .IsRequired();

            builder.Property(x => x.InventoryMode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue(TenantInventoryModes.TrackStock)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

        }
    }
}
