
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Domain.Entities;

    public class User
    {
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Tenant Tenant { get; private set; } = default!;

    private User() { }

    public static User Create(Guid tenantId, string email, string displayName, UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            DisplayName = displayName,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }
}

