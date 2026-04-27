using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Incidents.CreateIncident;

public static class CreateIncidentHandler
{
    public static async Task<CreateIncidentResponse> HandleAsync(
        CreateIncidentCommand command,
        DbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var incident = Incident.Create(
            tenantId: tenantId,
            serviceId: command.ServiceId,
            title: command.Title,
            severity: command.Severity,
            rawPayload: command.RawPayload);

        db.Set<Incident>().Add(incident);
        await db.SaveChangesAsync(ct);

        return new CreateIncidentResponse(incident.Id);
    }
}
