using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Incidents.GetIncidentDetail;

public static class GetIncidentDetailHandler
{
    public static Task<IncidentDetailDto?> HandleAsync(
        GetIncidentDetailQuery query,
        DbContext db,
        CancellationToken ct) =>
        db.Set<Incident>()
            .Where(i => i.Id == query.IncidentId)
            .Select(i => new IncidentDetailDto(
                i.Id,
                i.ServiceId,
                i.Service.Name,
                i.Title,
                i.Severity,
                i.Status,
                i.CreatedAt,
                i.AcknowledgedAt,
                i.ResolvedAt,
                i.AcknowledgedBy != null ? i.AcknowledgedBy.DisplayName : null,
                i.EscalationExhaustedAt,
                i.Service.EscalationPolicy != null ? i.Service.EscalationPolicy.Name : null,
                i.Events
                    .OrderBy(e => e.OccurredAt)
                    .Select(e => new IncidentEventDto(e.EventType, e.Description, e.OccurredAt))
                    .ToList(),
                // A subquery rather than a navigation: IncidentEscalation deliberately has no
                // inverse collection on Incident, so escalation history cannot be dragged
                // along by an unrelated Include.
                db.Set<IncidentEscalation>()
                    .Where(escalation => escalation.IncidentId == i.Id)
                    .OrderBy(escalation => escalation.Level)
                    .Select(escalation => new IncidentEscalationDto(
                        escalation.Level,
                        escalation.Tier.EscalateAfterMinutes,
                        escalation.DueAt,
                        escalation.NotifiedAt,
                        escalation.Attempts,
                        escalation.Targets
                            .Select(target => new EscalationRecipientDto(
                                target.UserId,
                                target.User.DisplayName,
                                target.User.Email,
                                target.Source))
                            .ToList()))
                    .ToList()))
            .FirstOrDefaultAsync(ct);
}
