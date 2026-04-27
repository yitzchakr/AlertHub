using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Tenants.DeleteTenant;

public static class DeleteTenantHandler
{
    public static async Task<bool> HandleAsync(
        DeleteTenantCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var tenant = await db.Set<Tenant>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == command.Id, ct);

        if (tenant is null)
            return false;

        db.Set<Tenant>().Remove(tenant);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
