using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.EscalationPolicies.CreateEscalationPolicy;

public static class CreateEscalationPolicyHandler
{
    public static async Task<CreateEscalationPolicyResponse> HandleAsync(
        CreateEscalationPolicyCommand command,
        DbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var policy = EscalationPolicy.Create(tenantId, command.Name);

        db.Set<EscalationPolicy>().Add(policy);
        await db.SaveChangesAsync(ct);

        return new CreateEscalationPolicyResponse(policy.Id, policy.Name);
    }
}
