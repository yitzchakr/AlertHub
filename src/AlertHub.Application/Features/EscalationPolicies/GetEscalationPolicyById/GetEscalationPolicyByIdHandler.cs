using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.EscalationPolicies.GetEscalationPolicyById;

public static class GetEscalationPolicyByIdHandler
{
    public static async Task<EscalationPolicyDetailDto?> HandleAsync(
        GetEscalationPolicyByIdQuery query,
        DbContext db,
        CancellationToken ct)
    {
        return await db.Set<EscalationPolicy>()
            .Where(p => p.Id == query.PolicyId)
            // Tiers and their assignments are two nested collections, which a single query
            // would return as a cross product. Read-only, so the extra round trips cost
            // nothing that matters.
            .AsSplitQuery()
            .Select(p => new EscalationPolicyDetailDto(
                p.Id,
                p.Name,
                p.Tiers.OrderBy(t => t.Level).Select(t => new EscalationTierDto(
                    t.Id,
                    t.Level,
                    t.EscalateAfterMinutes,
                    t.Assignments.Select(a => new EscalationTierAssignmentDto(
                        a.Id,
                        a.UserId,
                        a.User != null ? a.User.DisplayName : null,
                        a.User != null ? a.User.Email : null,
                        a.Role,
                        a.UseOnCallSchedule
                    )).ToList()
                )).ToList()
            ))
            .FirstOrDefaultAsync(ct);
    }
}
