using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Features.Services.CreateService;

public static class CreateServiceHandler
{
    public static async Task<CreateServiceResponse> HandleAsync(
        CreateServiceCommand command,
        DbContext db,
        CancellationToken ct)
    {
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var service = Service.Create(tenantId, command.Name, command.Description);

        db.Set<Service>().Add(service);
        await db.SaveChangesAsync(ct);

        return new CreateServiceResponse(service.Id, service.Name, service.Description, service.CreatedAt);
    }
}
