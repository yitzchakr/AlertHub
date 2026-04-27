using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Tenants.UpdateTenant;

public static class UpdateTenantHandler
{
    public static async Task<TenantSummaryDto?> HandleAsync(
        UpdateTenantCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var tenant = await db.Set<Tenant>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == command.Id, ct);

        if (tenant is null)
            return null;

        tenant.UpdateName(command.Name);
        await db.SaveChangesAsync(ct);

        return new TenantSummaryDto(tenant.Id, tenant.Name, tenant.CreatedAt);
    }
}
