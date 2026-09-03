namespace AlertHub.Application.Features.Escalations;

public class EscalationSettings
{
    /// <summary>Lets the worker be turned off without a deployment.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How long an incident may wait before its first tier fires, and the granularity of
    /// every later tier's timeout.
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Sends per escalation before giving up. Bounds the at-least-once retry loop: a
    /// transport that fails after delivering would otherwise re-page every tick forever.
    /// </summary>
    public int MaxSendAttempts { get; set; } = 3;
}
