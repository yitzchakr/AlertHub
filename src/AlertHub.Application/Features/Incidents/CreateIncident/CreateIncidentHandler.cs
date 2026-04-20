using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Incidents.CreateIncident;


public static class CreateIncidentHandler
{

    public static async Task<CreateIncidentResponse> HandleAsync(
        CreateIncidentCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var  _tempTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"); //replace with actual tenant ID in real implementation

        var incident = Incident.Create(
            tenantId: _tempTenantId,
            serviceId: command.ServiceId,
            title: command.Title,
            severity: command.Severity,
            rawPayload: command.RawPayload);
       

        var firstEvent = IncidentEvent.Create(
            tenantId: _tempTenantId,
            incidentId: incident.Id,
            type: IncidentEventType.Created,
            description: $"Incident created with severity {command.Severity}");
           
      

        db.Set<Incident>().Add(incident);
        db.Set<IncidentEvent>().Add(firstEvent);
        await db.SaveChangesAsync(ct);

        return new CreateIncidentResponse(incident.Id);
    }
}
