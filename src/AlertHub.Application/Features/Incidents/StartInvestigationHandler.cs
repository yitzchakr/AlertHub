using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Incidents;

public static class StartInvestigationHandler
{
        public static async Task HandleAsync(Guid incidentId, DbContext db, CancellationToken ct)
        {
            var incident = await db.Set<Incident>().FindAsync([incidentId], ct);
            if (incident == null)
            {
                throw new InvalidOperationException($"Incident with ID {incidentId} not found.");
            }
            incident.StartInvestigation();
            await db.SaveChangesAsync(ct);
        }
}
