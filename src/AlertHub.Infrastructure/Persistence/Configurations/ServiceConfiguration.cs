using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace AlertHub.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(1000);

        builder.HasMany(s => s.Incidents)
            .WithOne(i => i.Service)
            .HasForeignKey(i => i.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.EscalationPolicy)
            .WithMany(ep => ep.Services)
            .HasForeignKey(s => s.EscalationPolicyId)
            .OnDelete(DeleteBehavior.SetNull);
    }

}
