using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Incidents.ResolveIncident;

public static class ResolveIncidentHandler
{
    public static async Task HandleAsync(
        ResolveIncidentCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var incident = await db.Set<Incident>()
            .FirstOrDefaultAsync(i => i.Id == command.IncidentId, ct)
            ?? throw new InvalidOperationException($"Incident '{command.IncidentId}' not found.");

        incident.Resolve(command.Resolution);
        await db.SaveChangesAsync(ct);
    }
}
