using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Domain.Entities;

public class EscalationPolicy
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;

    // Navigation
    public Tenant Tenant { get; private set; } = default!;
    public ICollection<Service> Services { get; private set; } = [];
    public ICollection<EscalationTier> Tiers { get; private set; } = [];

    private EscalationPolicy() { }

    public static EscalationPolicy Create(Guid tenantId, string name)
    {
        return new EscalationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name
        };
    }

    public EscalationTier AddTier(int order, int escalateAfterMinutes)
    {
        if (Tiers.Any(t => t.Level == order))
            throw new InvalidOperationException(
                $"Tier with order {order} already exists.");

        var tier = EscalationTier.Create(Id, TenantId, order, escalateAfterMinutes);
        Tiers.Add(tier);
        return tier;
    }

    public EscalationTier? GetNextTier(int currentOrder)
    {
        return Tiers
            .Where(t => t.Level > currentOrder)
            .OrderBy(t => t.Level)
            .FirstOrDefault();
    }

}
