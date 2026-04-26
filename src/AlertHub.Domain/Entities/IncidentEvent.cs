using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Domain.Entities;

public class IncidentEvent : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid IncidentId { get; private set; }
    public Guid TenantId { get; private set; }
    public IncidentEventType EventType { get; private set; }
    public string Description { get; private set; } = default!;
    public DateTime OccurredAt { get; private set; }

    // Navigation
    public Incident Incident { get; private set; } = default!;

    private IncidentEvent() { }

    public static IncidentEvent Create(
        Guid incidentId, Guid tenantId,
        IncidentEventType type, string description)
    {
        return new IncidentEvent
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            TenantId = tenantId,
            EventType = type,
            Description = description,
            OccurredAt = DateTime.UtcNow
        };
    }
}
