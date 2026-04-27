using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Tenants.GetTenants;

public static class GetTenantsHandler
{
    public static async Task<List<TenantSummaryDto>> HandleAsync(
        GetTenantsQuery query,
        DbContext db,
        CancellationToken ct)
    {
        return await db.Set<Tenant>()
            .IgnoreQueryFilters()
            .Select(t => new TenantSummaryDto(t.Id, t.Name, t.CreatedAt))
            .ToListAsync(ct);
    }
}
