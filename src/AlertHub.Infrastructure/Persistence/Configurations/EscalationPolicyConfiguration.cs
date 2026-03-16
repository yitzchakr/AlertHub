using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertHub.Infrastructure.Persistence.Configurations;

public class EscalationPolicyConfiguration : IEntityTypeConfiguration<EscalationPolicy>
{
    public void Configure(EntityTypeBuilder<EscalationPolicy> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();

        // Configure the relationship without a navigation on Tenant
        builder.HasOne(p => p.Tenant)
            .WithMany()           // No collection navigation on Tenant
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Tiers)
            .WithOne(t => t.Policy)
            .HasForeignKey(t => t.EscalationPolicyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
