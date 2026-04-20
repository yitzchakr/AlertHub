using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Features.Incidents.AcknowledgeIncident;

public record AcknowledgeIncidentCommand
{
    public Guid IncidentId { get; init; }
    public Guid UserId { get; init; }
}
