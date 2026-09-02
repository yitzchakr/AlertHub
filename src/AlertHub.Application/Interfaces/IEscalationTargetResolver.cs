using AlertHub.Application.Features.EscalationPolicies;

namespace AlertHub.Application.Interfaces;

public interface IEscalationTargetResolver
{
    /// <summary>
    /// The concrete people an escalation tier reaches, for a given service at a given
    /// instant. serviceId is a parameter rather than read from the tier because an
    /// assignment carries no service — it comes from the incident being escalated.
    ///
    /// An empty result means the tier is configured but reaches nobody right now.
    /// </summary>
    Task<IReadOnlyList<EscalationTargetDto>> ResolveTierTargetsAsync(
        Guid tierId, Guid serviceId, DateTime atUtc, CancellationToken ct);
}
