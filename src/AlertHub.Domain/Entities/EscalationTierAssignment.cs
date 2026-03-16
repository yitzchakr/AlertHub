using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Domain.Entities;

public class EscalationTierAssignment
{
    public Guid Id { get; private set; }
    public Guid EscalationTierId { get; private set; }
    public Guid TenantId { get; private set; }

    // Assign either a specific user OR a role, not both
    public Guid? UserId { get; private set; }
    public UserRole? Role { get; private set; }

    // Navigation
    public EscalationTier Tier { get; private set; } = default!;
    public User? User { get; private set; }

    private EscalationTierAssignment() { }

    public static EscalationTierAssignment ForUser(
        Guid tierId, Guid tenantId, Guid userId)
    {
        return new EscalationTierAssignment
        {
            Id = Guid.NewGuid(),
            EscalationTierId = tierId,
            TenantId = tenantId,
            UserId = userId
        };
    }

    public static EscalationTierAssignment ForRole(
        Guid tierId, Guid tenantId, UserRole role)
    {
        return new EscalationTierAssignment
        {
            Id = Guid.NewGuid(),
            EscalationTierId = tierId,
            TenantId = tenantId,
            Role = role
        };
    }
}
