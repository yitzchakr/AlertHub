namespace AlertHub.Application.Features.EscalationPolicies.GetTierTargets;

/// <summary>
/// Answers "who would this tier page?" — the pre-incident audit of an escalation policy.
/// </summary>
public record GetTierTargetsQuery(Guid TierId, Guid ServiceId, DateTime? AtUtc = null);
