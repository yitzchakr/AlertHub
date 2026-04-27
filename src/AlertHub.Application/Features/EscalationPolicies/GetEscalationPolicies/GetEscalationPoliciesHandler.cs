using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.EscalationPolicies.GetEscalationPolicies;

public static class GetEscalationPoliciesHandler
{
    public static async Task<List<EscalationPolicySummaryDto>> HandleAsync(
        GetEscalationPoliciesQuery query,
        DbContext db,
        CancellationToken ct)
    {
        return await db.Set<EscalationPolicy>()
            .Select(p => new EscalationPolicySummaryDto(p.Id, p.Name, p.Tiers.Count))
            .ToListAsync(ct);
    }
}
