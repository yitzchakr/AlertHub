using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Incidents.AcknowledgeIncident;

public static class AcknowledgeIncidentHandler

{
    public static async Task HandleAsync( AcknowledgeIncidentCommand command, DbContext db, CancellationToken ct)
    {

        var incident = await db.Set<Incident>().FindAsync([command.IncidentId], ct);
        if (incident == null)
        {
            throw new InvalidOperationException($"Incident with ID {command.IncidentId} not found.");
        }
        incident.Acknowledge(command.UserId);
        await db.SaveChangesAsync(ct);
 
    }
}
