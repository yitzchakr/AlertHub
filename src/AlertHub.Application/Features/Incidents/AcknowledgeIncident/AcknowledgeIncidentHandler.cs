using AlertHub.Domain.Entities;
using AlertHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Incidents.AcknowledgeIncident;

public static class AcknowledgeIncidentHandler

{
    public static async Task HandleAsync( AcknowledgeIncidentCommand command, DbContext db, CancellationToken ct)
    {

        var incident = await db.Set<Incident>()
            .FirstOrDefaultAsync(i => i.Id == command.IncidentId, ct)
            ?? throw NotFoundException.For<Incident>(command.IncidentId);

        var acknowledgedBy = await db.Set<User>()
            .Where(u => u.Id == command.UserId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct)
            ?? throw NotFoundException.For<User>(command.UserId);

        incident.Acknowledge(command.UserId, acknowledgedBy);
        await db.SaveChangesAsync(ct);
 
    }
}
