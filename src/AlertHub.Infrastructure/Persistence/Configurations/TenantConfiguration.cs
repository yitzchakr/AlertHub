using System;
using System.Collections.Generic;
using System.Text;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertHub.Infrastructure.Persistence.Configurations;

   public class TenantConfiguration: IEntityTypeConfiguration<Tenant>

    {
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(128).IsRequired();

        builder.HasMany(t => t.Users).
            WithOne(u => u.Tenant).
            HasForeignKey(u => u.TenantId).
            OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Services).
            WithOne(s => s.Tenant).
            HasForeignKey(s => s.TenantId).
            OnDelete(DeleteBehavior.Restrict);

    }

    }

