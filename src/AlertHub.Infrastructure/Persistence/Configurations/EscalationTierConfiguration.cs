using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Infrastructure.Persistence.Configurations;

public class EscalationTierConfiguration : IEntityTypeConfiguration<EscalationTier>
{
    public void Configure(EntityTypeBuilder<EscalationTier> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Level).IsRequired();
        builder.Property(t => t.EscalateAfterMinutes).IsRequired();

        builder.HasMany(t => t.Assignments)
            .WithOne(a => a.Tier)
            .HasForeignKey(a => a.EscalationTierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }

}
