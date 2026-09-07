using AlertHub.Domain.Entities;

namespace AlertHub.Application.Features.Incidents;

/// <summary>
/// One incident with everything that has happened to it. The escalation history is the
/// point: without it, an incident that escalated through every tier and reached nobody
/// looks identical to one that is quietly fine.
/// </summary>
public record IncidentDetailDto(
    Guid Id,
    Guid ServiceId,
    string ServiceName,
    string Title,
    IncidentSeverity Severity,
    IncidentStatus Status,
    DateTime CreatedAt,
    DateTime? AcknowledgedAt,
    DateTime? ResolvedAt,
    string? AcknowledgedByDisplayName,
    DateTime? EscalationExhaustedAt,
    string? EscalationPolicyName,
    IReadOnlyList<IncidentEventDto> Events,
    IReadOnlyList<IncidentEscalationDto> Escalations);

public record IncidentEventDto(
    IncidentEventType EventType,
    string Description,
    DateTime OccurredAt);

public record IncidentEscalationDto(
    int Level,
    int EscalateAfterMinutes,
    DateTime DueAt,
    DateTime? NotifiedAt,
    int Attempts,
    IReadOnlyList<EscalationRecipientDto> Targets);

public record EscalationRecipientDto(
    Guid UserId,
    string DisplayName,
    string Email,
    EscalationTargetSource Source);
