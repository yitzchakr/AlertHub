namespace AlertHub.Application.Features.Escalations;

/// <summary>
/// What one pass did, so it can be logged in terms of what changed rather than how many
/// rows it looked at. A pass that only waited is not worth a log line.
/// </summary>
public class EscalationPassSummary
{
    public int Considered { get; init; }
    public int Notified { get; private set; }
    public int ReachedNobody { get; private set; }
    public int Exhausted { get; private set; }
    public int GaveUp { get; private set; }
    public int Skipped { get; private set; }
    public int NotYetDue { get; private set; }
    public int Failed { get; set; }

    /// <summary>True when something actually happened, as opposed to a quiet tick.</summary>
    public bool HasNews =>
        Notified > 0 || ReachedNobody > 0 || Exhausted > 0 || GaveUp > 0 || Failed > 0;

    public void Record(EscalationOutcome outcome)
    {
        switch (outcome)
        {
            case EscalationOutcome.Notified: Notified++; break;
            case EscalationOutcome.ReachedNobody: ReachedNobody++; break;
            case EscalationOutcome.Exhausted: Exhausted++; break;
            case EscalationOutcome.GaveUp: GaveUp++; break;
            case EscalationOutcome.NotYetDue: NotYetDue++; break;
            default: Skipped++; break;
        }
    }

    public override string ToString() =>
        $"considered {Considered}, notified {Notified}, reached nobody {ReachedNobody}, "
      + $"exhausted {Exhausted}, gave up {GaveUp}, waiting {NotYetDue}, "
      + $"skipped {Skipped}, failed {Failed}";
}
