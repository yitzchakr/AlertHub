using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.EscalationPolicies.DeleteEscalationPolicy;

public static class DeleteEscalationPolicyHandler
{
    public static async Task<bool> HandleAsync(
        DeleteEscalationPolicyCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var policy = await db.Set<EscalationPolicy>()
            .FirstOrDefaultAsync(p => p.Id == command.PolicyId, ct);

        if (policy is null)
            return false;

        db.Set<EscalationPolicy>().Remove(policy);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
