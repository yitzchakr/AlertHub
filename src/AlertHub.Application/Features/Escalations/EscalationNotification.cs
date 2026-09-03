using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Domain.Entities;

namespace AlertHub.Application.Features.Escalations;

/// <summary>
/// Everything a sender needs to tell somebody an incident needs them. Recipients may be
/// empty, which is the roster having a gap — a sender should say so rather than treat it
/// as an error.
/// </summary>
public record EscalationNotification(
    Guid IncidentId,
    string IncidentTitle,
    IncidentSeverity Severity,
    Guid ServiceId,
    string ServiceName,
    int Level,
    IReadOnlyList<EscalationTargetDto> Recipients);
