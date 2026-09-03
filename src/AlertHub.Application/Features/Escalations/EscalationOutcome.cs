namespace AlertHub.Application.Features.Escalations;

/// <summary>
/// What one pass over one incident did. Reported so a pass can be logged in terms of what
/// changed rather than how many rows it looked at.
/// </summary>
public enum EscalationOutcome
{
    /// <summary>Somebody took it, it has no policy, or another instance claimed the tier.</summary>
    Skipped,

    /// <summary>The next tier exists but its timeout has not elapsed.</summary>
    NotYetDue,

    Notified,

    /// <summary>Sent, but the tier resolved to no people — a coverage gap.</summary>
    ReachedNobody,

    /// <summary>Sending failed too many times; the page is dropped.</summary>
    GaveUp,

    /// <summary>No further tier exists. Escalation stops here.</summary>
    Exhausted
}
