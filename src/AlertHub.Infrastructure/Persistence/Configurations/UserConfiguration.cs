
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlertHub.Infrastructure.Persistence.Configurations;

    public class UserConfiguration : IEntityTypeConfiguration<User>
{


    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(300).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PermissionRole).HasConversion<string>().HasMaxLength(50);
        builder.Property(u => u.EscalationRole).HasConversion<string>().HasMaxLength(50);
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.RefreshToken).HasMaxLength(500);
        builder.Property(u => u.RefreshTokenExpiryTime);

        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
    }
}

