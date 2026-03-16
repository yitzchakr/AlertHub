using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Domain.Entities;

public enum IncidentStatus
{
    Triggered,
    Acknowledged,
    Investigating,
    Resolved
}