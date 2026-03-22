using AlertHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Features.Incidents.CreateIncident;

public record CreateIncidentCommand
       (Guid ServiceId,
        string Title,
        IncidentSeverity Severity,
        string? RawPayload);

public record CreateIncidentResponse(Guid IncidentId);
