using System;
using System.Collections.Generic;
using System.Text;


namespace AlertHub.Domain.Entities;

public enum IncidentEventType
{
    Created,
    Acknowledged,
    InvestigationStarted,
    Escalated,
    Resolved,
    NoteAdded,
    AiAnalysisCompleted
}