using AlertHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Features.Incidents;

public record IncidentSummaryDto
(   Guid Id,
    string Title,
    IncidentSeverity Severity,
    IncidentStatus Status,
    DateTime CreatedAt
);
