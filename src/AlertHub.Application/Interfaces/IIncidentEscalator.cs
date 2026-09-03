using AlertHub.Application.Features.Escalations;

namespace AlertHub.Application.Interfaces;

public interface IIncidentEscalator
{
    /// <summary>
    /// Advances one incident by at most one escalation step. Must be called inside a
    /// tenant-scoped DI scope; it throws rather than quietly finding nothing otherwise.
    /// </summary>
    Task<EscalationOutcome> EscalateAsync(Guid incidentId, DateTime utcNow, CancellationToken ct);
}
