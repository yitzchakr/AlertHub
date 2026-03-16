using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertHub.Infrastructure.Persistence.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Title).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(i => i.Severity).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(i => i.AcknowledgedBy)
            .WithMany()
            .HasForeignKey(i => i.AcknowledgedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.Events)
            .WithOne(e => e.Incident)
            .HasForeignKey(e => e.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for common queries
        builder.HasIndex(i => new { i.TenantId, i.ServiceId, i.Status });
    }
}
