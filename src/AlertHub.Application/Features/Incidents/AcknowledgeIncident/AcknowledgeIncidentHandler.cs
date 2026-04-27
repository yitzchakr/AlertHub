using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Incidents.AcknowledgeIncident;

public static class AcknowledgeIncidentHandler

{
    public static async Task HandleAsync( AcknowledgeIncidentCommand command, DbContext db, CancellationToken ct)
    {

        var incident = await db.Set<Incident>()
            .FirstOrDefaultAsync(i => i.Id == command.IncidentId, ct)
            ?? throw new InvalidOperationException($"Incident '{command.IncidentId}' not found.");
        incident.Acknowledge(command.UserId);
        await db.SaveChangesAsync(ct);
 
    }
}
