using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Tenants.UpdateTenant;

public static class UpdateTenantHandler
{
    public static async Task<TenantSummaryDto?> HandleAsync(
        UpdateTenantCommand command,
        DbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");

        // Renaming somebody else's tenant is not a 403 here but a 404: the caller is not
        // told whether the id exists.
        if (command.Id != tenantId)
            return null;

        var tenant = await db.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
            return null;

        tenant.UpdateName(command.Name);
        await db.SaveChangesAsync(ct);

        return new TenantSummaryDto(tenant.Id, tenant.Name, tenant.CreatedAt);
    }
}
