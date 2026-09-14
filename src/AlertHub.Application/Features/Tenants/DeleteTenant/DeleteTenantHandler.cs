using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Tenants.DeleteTenant;

public static class DeleteTenantHandler
{
    public static async Task<bool> HandleAsync(
        DeleteTenantCommand command,
        DbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");

        // Deleting a tenant cascades to its services, incidents and users, so the one
        // tenant a caller may delete is their own.
        if (command.Id != tenantId)
            return false;

        var tenant = await db.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
            return false;

        db.Set<Tenant>().Remove(tenant);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
