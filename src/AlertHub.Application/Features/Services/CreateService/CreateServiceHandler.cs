using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Services.CreateService;

public static class CreateServiceHandler
{
    public static async Task<CreateServiceResponse> HandleAsync(
        CreateServiceCommand command,
        DbContext db,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var service = Service.Create(tenantId, command.Name, command.Description);

        db.Set<Service>().Add(service);
        await db.SaveChangesAsync(ct);

        return new CreateServiceResponse(service.Id, service.Name, service.Description, service.CreatedAt);
    }
}
