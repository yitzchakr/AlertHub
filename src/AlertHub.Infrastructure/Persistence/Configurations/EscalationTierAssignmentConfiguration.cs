using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertHub.Infrastructure.Persistence.Configurations;

public class EscalationTierAssignmentConfiguration : IEntityTypeConfiguration<EscalationTierAssignment>
{
    public void Configure(EntityTypeBuilder<EscalationTierAssignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EscalationRole).HasConversion<string>();
        builder.Property(a => a.UseOnCallSchedule).HasDefaultValue(false);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
