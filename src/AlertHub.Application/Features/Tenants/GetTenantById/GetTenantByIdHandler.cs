using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Tenants.GetTenantById;

public static class GetTenantByIdHandler
{
    /// <summary>
    /// Null for any tenant that is not the caller's, so a probe for another tenant's id is
    /// answered with the same 404 as an id that does not exist.
    /// </summary>
    public static async Task<TenantSummaryDto?> HandleAsync(
        GetTenantByIdQuery query,
        DbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");

        if (query.TenantId != tenantId)
            return null;

        return await db.Set<Tenant>()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantSummaryDto(t.Id, t.Name, t.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
