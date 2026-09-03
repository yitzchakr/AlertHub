namespace AlertHub.Domain.Entities;

/// <summary>
/// One person an escalation reached, and how they were selected.
/// </summary>
public readonly record struct EscalationRecipient(Guid UserId, EscalationTargetSource Source);
