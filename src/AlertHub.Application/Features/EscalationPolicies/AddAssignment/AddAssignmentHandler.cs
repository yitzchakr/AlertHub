using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.EscalationPolicies.AddAssignment;

public static class AddAssignmentHandler
{
    public static async Task<AddAssignmentResponse> HandleAsync(
        AddAssignmentCommand command,
        DbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var tier = await db.Set<EscalationTier>()
            .FirstOrDefaultAsync(t => t.Id == command.TierId, ct)
            ?? throw new InvalidOperationException($"Escalation tier '{command.TierId}' not found.");

        EscalationTierAssignment assignment;

        if (command.UseOnCallSchedule)
        {
            assignment = EscalationTierAssignment.ForOnCall(tier.Id, tenantId);
        }
        else if (command.UserId.HasValue)
        {
            assignment = EscalationTierAssignment.ForUser(tier.Id, tenantId, command.UserId.Value);
        }
        else if (command.EscalationRole.HasValue)
        {
            assignment = EscalationTierAssignment.ForRole(tier.Id, tenantId, command.EscalationRole.Value);
        }
        else
        {
            throw new InvalidOperationException(
                "Assignment must target a user, a role, or the on-call schedule.");
        }

        db.Set<EscalationTierAssignment>().Add(assignment);
        await db.SaveChangesAsync(ct);

        return new AddAssignmentResponse(assignment.Id);
    }
}
