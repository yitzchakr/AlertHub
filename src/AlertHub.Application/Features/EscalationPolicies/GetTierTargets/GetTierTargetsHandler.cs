using AlertHub.Application.Interfaces;

namespace AlertHub.Application.Features.EscalationPolicies.GetTierTargets;

public static class GetTierTargetsHandler
{
    public static Task<IReadOnlyList<EscalationTargetDto>> HandleAsync(
        GetTierTargetsQuery query,
        IEscalationTargetResolver resolver,
        CancellationToken ct) =>
        resolver.ResolveTierTargetsAsync(
            query.TierId, query.ServiceId, query.AtUtc ?? DateTime.UtcNow, ct);
}
