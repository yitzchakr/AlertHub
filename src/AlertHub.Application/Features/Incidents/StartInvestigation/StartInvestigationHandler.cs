using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Incidents.StartInvestigation;

public static class StartInvestigationHandler
{
    public static async Task HandleAsync(
        StartInvestigationCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var incident = await db.Set<Incident>()
            .FirstOrDefaultAsync(i => i.Id == command.IncidentId, ct)
            ?? throw new InvalidOperationException($"Incident '{command.IncidentId}' not found.");

        incident.StartInvestigation();
        await db.SaveChangesAsync(ct);
    }
}
