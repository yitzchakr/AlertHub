using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertHub.Infrastructure.Persistence.Configurations;

public class IncidentEscalationConfiguration : IEntityTypeConfiguration<IncidentEscalation>
{
    public void Configure(EntityTypeBuilder<IncidentEscalation> builder)
    {
        builder.HasKey(e => e.Id);

        // The load-bearing constraint. Two API instances both running the worker will both
        // compute that a tier is due; the second insert violates this, that instance skips,
        // and nobody is paged twice. Without it, every extra instance means an extra page.
        builder.HasIndex(e => new { e.IncidentId, e.Level }).IsUnique();

        // Filtered, because the scan only ever wants unsent rows while almost every row
        // becomes sent. An unfiltered index would grow with the whole table to serve a
        // query that touches a handful of rows every tick.
        builder.HasIndex(e => e.NotifiedAt)
            .HasFilter("[NotifiedAt] IS NULL");

        builder.HasOne(e => e.Incident)
            .WithMany()
            .HasForeignKey(e => e.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tier)
            .WithMany()
            .HasForeignKey(e => e.EscalationTierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Targets)
            .WithOne(t => t.Escalation)
            .HasForeignKey(t => t.IncidentEscalationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
