using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Tenants.CreateTenant;

public static class CreateTenantHandler
{
    public static async Task<CreateTenantResponse> HandleAsync(
        CreateTenantCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var tenant = Tenant.Create(command.Name);

        db.Set<Tenant>().Add(tenant);
        await db.SaveChangesAsync(ct);

        return new CreateTenantResponse(tenant.Id, tenant.Name, tenant.CreatedAt);
    }
}
