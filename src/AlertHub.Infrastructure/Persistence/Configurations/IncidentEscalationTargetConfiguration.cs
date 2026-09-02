using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertHub.Infrastructure.Persistence.Configurations;

public class IncidentEscalationTargetConfiguration
    : IEntityTypeConfiguration<IncidentEscalationTarget>
{
    public void Configure(EntityTypeBuilder<IncidentEscalationTarget> builder)
    {
        builder.HasKey(t => t.Id);

        // Stored as text, matching how every other enum in this schema is persisted.
        builder.Property(t => t.Source).HasConversion<string>().HasMaxLength(30);

        // A person is recorded once per escalation. The resolver already dedupes and the
        // aggregate dedupes defensively; this makes it structural.
        builder.HasIndex(t => new { t.IncidentEscalationId, t.UserId }).IsUnique();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
