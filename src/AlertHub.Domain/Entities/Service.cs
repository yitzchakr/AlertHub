using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Domain.Entities;

public class Service
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? EscalationPolicyId { get; private set; }

    // Navigation
    public Tenant Tenant { get; private set; } = default!;
    public ICollection<Incident> Incidents { get; private set; } = [];
    public EscalationPolicy? EscalationPolicy { get; private set; }

    private Service() { }

    public static Service Create(Guid tenantId, string name, string? description = null, Guid? escalationPolicyId = null)
    {
        return new Service
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            EscalationPolicyId = escalationPolicyId
        };
    }
    public void SetEscalationPolicy(Guid? escalationPolicyId)
    {
        EscalationPolicyId = escalationPolicyId;
    }
}

