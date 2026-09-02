using System;
using System.Collections.Generic;
using System.Text;

using AlertHub.Domain.Exceptions;

namespace AlertHub.Domain.Entities;

public class Incident : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ServiceId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? RawAlertPayload { get; private set; }
    public IncidentSeverity Severity { get; private set; }
    public IncidentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }

    // Navigation
    public Tenant Tenant { get; private set; } = default!;
    public Service Service { get; private set; } = default!;
    public User? AcknowledgedBy { get; private set; }
    public ICollection<IncidentEvent> Events { get; private set; } = [];

    private Incident() { }

    public static Incident Create(
        Guid tenantId, Guid serviceId, string title,
        IncidentSeverity severity, string? rawPayload = null)
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            Title = title,
            RawAlertPayload = rawPayload,
            Severity = severity,
            Status = IncidentStatus.Triggered,
            CreatedAt = DateTime.UtcNow
        };

        incident.AddEvent(IncidentEventType.Created, "Incident triggered");
        return incident;
    }

    // --- State transition methods with guard clauses ---

    public void Acknowledge(Guid userId)
    {
        if (Status != IncidentStatus.Triggered)
            throw new InvalidOperationException(
                $"Cannot acknowledge incident in '{Status}' state. Must be 'Triggered'.");

        Status = IncidentStatus.Acknowledged;
        AcknowledgedAt = DateTime.UtcNow;
        AcknowledgedByUserId = userId;
        AddEvent(IncidentEventType.Acknowledged, $"Acknowledged by user {userId}");
    }

    public void StartInvestigation()
    {
        if (Status != IncidentStatus.Acknowledged)
            throw new InvalidOperationException(
                $"Cannot investigate incident in '{Status}' state. Must be 'Acknowledged'.");

        Status = IncidentStatus.Investigating;
        AddEvent(IncidentEventType.InvestigationStarted, "Investigation started");
    }

    public void Resolve(string resolution)
    {
        if (Status is IncidentStatus.Resolved or IncidentStatus.Triggered)
            throw new InvalidOperationException(
                $"Cannot resolve incident in '{Status}' state.");

        Status = IncidentStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        AddEvent(IncidentEventType.Resolved, resolution);
    }

    /// <summary>
    /// Writes an escalation onto the incident's timeline. The status guard is the
    /// authoritative stop condition: a scan can decide an incident is due and an
    /// acknowledgement can land before the write, and this is what refuses it.
    /// </summary>
    public void RecordEscalation(int level, string description)
    {
        if (Status != IncidentStatus.Triggered)
            throw new DomainException(
                $"Cannot escalate an incident in '{Status}' state. Escalation stops once "
              + "somebody has taken it.");

        AddEvent(IncidentEventType.Escalated, description);
    }

    private void AddEvent(IncidentEventType type, string description)
    {
        Events.Add(IncidentEvent.Create(Id, TenantId, type, description));
    }
}
