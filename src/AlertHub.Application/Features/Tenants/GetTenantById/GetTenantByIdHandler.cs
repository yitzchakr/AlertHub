using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Tenants.GetTenantById;

public static class GetTenantByIdHandler
{
    public static async Task<TenantSummaryDto?> HandleAsync(
        GetTenantByIdQuery query,
        DbContext db,
        CancellationToken ct)
    {
        return await db.Set<Tenant>()
            .IgnoreQueryFilters()
            .Where(t => t.Id == query.TenantId)
            .Select(t => new TenantSummaryDto(t.Id, t.Name, t.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
