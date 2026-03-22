using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertHub.Infrastructure.Persistence.Configurations;

public class OnCallScheduleConfiguration
    : IEntityTypeConfiguration<OnCallSchedule>
{
    public void Configure(EntityTypeBuilder<OnCallSchedule> builder)
    {
        builder.HasKey(s => s.Id);

        builder.OwnsOne(s => s.Window, window =>
        {
            window.Property(w => w.Start)
                .HasColumnName("WindowStart");
            window.Property(w => w.End)
                .HasColumnName("WindowEnd");
        });

        builder.HasOne(s => s.Service)
            .WithMany()
            .HasForeignKey(s => s.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Tenant)
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
