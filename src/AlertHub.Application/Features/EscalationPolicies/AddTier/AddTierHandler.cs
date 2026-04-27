using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.EscalationPolicies.AddTier;

public static class AddTierHandler
{
    public static async Task<AddTierResponse> HandleAsync(
        AddTierCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var policy = await db.Set<EscalationPolicy>()
            .Include(p => p.Tiers)
            .FirstOrDefaultAsync(p => p.Id == command.PolicyId, ct)
            ?? throw new InvalidOperationException($"Escalation policy '{command.PolicyId}' not found.");

        var tier = policy.AddTier(command.Level, command.EscalateAfterMinutes);
        await db.SaveChangesAsync(ct);

        return new AddTierResponse(tier.Id, tier.Level, tier.EscalateAfterMinutes);
    }
}
