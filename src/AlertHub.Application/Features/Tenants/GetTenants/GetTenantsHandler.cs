using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Tenants.GetTenants;

public static class GetTenantsHandler
{
    /// <summary>
    /// The caller's own tenant, as a list because a caller may one day belong to more than
    /// one. Tenant is not an ITenantEntity, so no global query filter applies to it and the
    /// scoping has to be written out: without it this returns every tenant on the
    /// installation, which is how a Viewer would collect the ids needed to reach across the
    /// boundary everything else is careful about.
    /// </summary>
    public static async Task<List<TenantSummaryDto>> HandleAsync(
        GetTenantsQuery query,
        DbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return await db.Set<Tenant>()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantSummaryDto(t.Id, t.Name, t.CreatedAt))
            .ToListAsync(ct);
    }
}
