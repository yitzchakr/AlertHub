using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Domain.Entities;

public class EscalationTierAssignment : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid EscalationTierId { get; private set; }
    public Guid TenantId { get; private set; }

    // Each assignment targets exactly one of: a specific user, a role, or the on-call schedule.
    // A tier can have multiple assignments to notify several targets simultaneously.
    public Guid? UserId { get; private set; }
    public UserRole? Role { get; private set; }
    public bool UseOnCallSchedule { get; private set; }


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
    public static EscalationTierAssignment ForOnCall(
    Guid tierId, Guid tenantId)
    {
        return new EscalationTierAssignment
        {
            Id = Guid.NewGuid(),
            EscalationTierId = tierId,
            TenantId = tenantId,
            UseOnCallSchedule = true
        };
    }
}
