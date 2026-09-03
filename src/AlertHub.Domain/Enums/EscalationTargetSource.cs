namespace AlertHub.Domain.Entities;

/// <summary>
/// How an escalation tier selected a person. Persisted on IncidentEscalationTarget, which
/// is why it lives in the domain rather than alongside the read model that also exposes it.
/// </summary>
public enum EscalationTargetSource
{
    DirectUser,
    Role,
    OnCall
}
