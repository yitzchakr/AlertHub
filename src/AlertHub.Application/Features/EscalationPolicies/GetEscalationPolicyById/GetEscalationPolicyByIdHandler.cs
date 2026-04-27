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
                        a.Role,
                        a.UseOnCallSchedule
                    )).ToList()
                )).ToList()
            ))
            .FirstOrDefaultAsync(ct);
    }
}
