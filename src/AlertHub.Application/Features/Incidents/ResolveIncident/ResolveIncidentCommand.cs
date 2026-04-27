namespace AlertHub.Application.Features.Incidents.ResolveIncident;

public record ResolveIncidentCommand(Guid IncidentId, string Resolution);
